using ksts.be.applications.LoKy.Interfaces;
using ksts.be.external.Storage.Interfaces;
using ksts.be.infrastructure.Persistence;
using ksts.be.shared.Constants;
using ksts.be.shared.Constants.LoKy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace ksts.be.applications.LoKy.Implements
{
    /// <inheritdoc/>
    public class DayLenKhoRunner : IDayLenKhoRunner
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IS3FileStorage _s3FileStorage;
        private readonly ILogger<DayLenKhoRunner> _logger;

        private readonly ConcurrentDictionary<int, CancellationTokenSource> _dangChay = new();

        public DayLenKhoRunner(
            IServiceScopeFactory scopeFactory,
            IS3FileStorage s3FileStorage,
            ILogger<DayLenKhoRunner> logger)
        {
            _scopeFactory = scopeFactory;
            _s3FileStorage = s3FileStorage;
            _logger = logger;
        }

        /// <inheritdoc/>
        public void BatDau(int loKyId)
        {
            _logger.LogInformation("{Method} loKyId={LoKyId}", nameof(BatDau), loKyId);

            var nguon = new CancellationTokenSource();
            if (!_dangChay.TryAdd(loKyId, nguon))
            {
                nguon.Dispose();
                return;
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    await ChayAsync(loKyId, nguon.Token);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Đẩy lô {LoKyId} lên kho dừng vì sự cố chung", loKyId);
                    await ChotTrangThaiAsync(loKyId, ex.Message);
                }
                finally
                {
                    if (_dangChay.TryRemove(loKyId, out var xong)) xong.Dispose();
                }
            });
        }

        /// <inheritdoc/>
        public bool DangChay(int loKyId) => _dangChay.ContainsKey(loKyId);

        /// <inheritdoc/>
        public async Task ChayAsync(int loKyId, CancellationToken cancellationToken)
        {
            List<int> fileIds;
            using (var scope = _scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<KstsDbContext>();

                // Chỉ đẩy file đã ký XONG: file lỗi hoặc còn dở thì chưa có bản ký để đẩy.
                fileIds = await db.LoKyFile
                    .Where(x => x.LoKyId == loKyId && !x.Deleted && x.TrangThai == TrangThaiFileKy.Xong
                        && x.ObjectKeyDaKy != null)
                    .OrderBy(x => x.ThuTu)
                    .Select(x => x.Id)
                    .ToListAsync(cancellationToken);
            }

            var ke = -1;
            var luong = Enumerable.Range(0, LoKyConstants.SoFileDayLenKhoSongSong)
                .Select(_ => Task.Run(async () =>
                {
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var thuTu = Interlocked.Increment(ref ke);
                        if (thuTu >= fileIds.Count)
                        {
                            break;
                        }

                        await DayMotFileAsync(loKyId, fileIds[thuTu], cancellationToken);
                    }
                }, cancellationToken))
                .ToArray();

            try
            {
                await Task.WhenAll(luong);
            }
            catch (OperationCanceledException)
            {
            }

            await ChotTrangThaiAsync(loKyId, null);
        }

        /// <inheritdoc/>
        public async Task DayMotFileAsync(int loKyId, int loKyFileId, CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<KstsDbContext>();

                var file = await db.LoKyFile
                    .FirstOrDefaultAsync(x => x.Id == loKyFileId, cancellationToken);

                if (file?.ObjectKeyDaKy == null)
                {
                    return;
                }

                var khoaCu = file.ObjectKeyDaKy;
                var khoaMoi = LoKyConstants.GetKhoDaKyKey(file.TenFile);
                if (string.Equals(khoaCu, khoaMoi, StringComparison.Ordinal))
                {
                    await CongDonAsync(loKyId, true);
                    return;
                }

                // Chép THẲNG trong kho: bản đã ký vốn đã nằm sẵn ở đó, kéo về server rồi đẩy ngược lên là
                // cho vài GB đi qua đường truyền thêm hai lần mà không được gì.
                await _s3FileStorage.CopyAsync(khoaCu, khoaMoi, cancellationToken);

                // CHUYỂN chứ không phải chép thêm: trỏ lô sang chỗ mới rồi mới xoá bản trong lo-ky/, nhờ vậy
                // lô ký không để lại gì trên kho mà nút Tải zip vẫn đọc được vì nó đi theo ObjectKeyDaKy.
                // Ghi DB TRƯỚC khi xoá — đảo thứ tự là mất đứt file nếu tiến trình chết giữa hai bước.
                file.ObjectKeyDaKy = khoaMoi;
                file.ModifiedDate = DateTimeConstants.VietnamNow;
                await db.SaveChangesAsync(cancellationToken);

                await _s3FileStorage.DeleteAsync(khoaCu, cancellationToken);

                await CongDonAsync(loKyId, true);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Đẩy file {FileId} của lô {LoKyId} lên kho hỏng", loKyFileId, loKyId);
                await CongDonAsync(loKyId, false);
            }
        }

        /// <inheritdoc/>
        public async Task CongDonAsync(int loKyId, bool thanhCong)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<KstsDbContext>();
            var now = DateTimeConstants.VietnamNow;

            if (thanhCong)
            {
                await db.LoKy.Where(x => x.Id == loKyId)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.DaDayLenKho, x => x.DaDayLenKho + 1)
                        .SetProperty(x => x.ModifiedDate, now));
                return;
            }

            await db.LoKy.Where(x => x.Id == loKyId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.SoLoiDayLenKho, x => x.SoLoiDayLenKho + 1)
                    .SetProperty(x => x.ModifiedDate, now));
        }

        /// <summary>Chốt lại cờ khi lô đẩy xong hoặc gãy giữa chừng.</summary>
        public async Task ChotTrangThaiAsync(int loKyId, string? loi)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<KstsDbContext>();
            var now = DateTimeConstants.VietnamNow;

            await db.LoKy.Where(x => x.Id == loKyId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.DangDayLenKho, false)
                    .SetProperty(x => x.HoanTatDayLenKho, true)
                    .SetProperty(x => x.LoiDayLenKho, loi)
                    .SetProperty(x => x.ModifiedDate, now));
        }
    }
}
