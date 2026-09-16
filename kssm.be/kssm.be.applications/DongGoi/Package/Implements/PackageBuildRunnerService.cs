using kssm.be.applications.DongGoi.Package.Dtos;
using kssm.be.applications.DongGoi.Package.Interfaces;
using kssm.be.domain.DongGoi;
using kssm.be.external.DongGoi.Dtos;
using kssm.be.external.DongGoi.Interfaces;
using kssm.be.infrastructure.Persistence;
using kssm.be.shared.Constants;
using kssm.be.shared.Constants.DongGoi;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace kssm.be.applications.DongGoi.Package.Implements
{
    public class PackageBuildRunnerService : IPackageBuildRunnerService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<PackageBuildRunnerService> _logger;
        private readonly ConcurrentDictionary<int, CancellationTokenSource> _dangChay = new();

        public PackageBuildRunnerService(IServiceScopeFactory scopeFactory, ILogger<PackageBuildRunnerService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public bool DangChay(int sessionId) => _dangChay.ContainsKey(sessionId);

        public void Dung(int sessionId)
        {
            if (_dangChay.TryGetValue(sessionId, out var nguon))
            {
                nguon.Cancel();
            }
        }

        public void BatDau(int sessionId, DongGoiDto dto)
        {
            var nguon = new CancellationTokenSource();

            if (!_dangChay.TryAdd(sessionId, nguon))
            {
                nguon.Dispose();
                return;
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    await ChayAsync(sessionId, dto, nguon.Token);
                }
                catch (OperationCanceledException) when (nguon.IsCancellationRequested)
                {
                    await GhiLoiAsync(sessionId, "Đóng gói đã dừng giữa chừng.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"{nameof(ChayAsync)} sessionId={sessionId}");
                    await GhiLoiAsync(sessionId, ex.Message);
                }
                finally
                {
                    _dangChay.TryRemove(sessionId, out _);
                    nguon.Dispose();
                }
            });
        }

        public async Task ChayAsync(int sessionId, DongGoiDto dto, CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<KssmDbContext>();
            var builder = scope.ServiceProvider.GetRequiredService<IPackageBuilderService>();
            var schemaService = scope.ServiceProvider.GetRequiredService<IMetadataSchemaService>();
            var schemaTemplate = scope.ServiceProvider.GetRequiredService<IPackageSchemaTemplate>();
            var storage = scope.ServiceProvider.GetRequiredService<IPackageFileStorage>();

            var phien = await db.PackageSession
                .AsNoTracking()
                .FirstOrDefaultAsync(x => !x.Deleted && x.Id == sessionId, cancellationToken);

            if (phien == null || string.IsNullOrWhiteSpace(phien.DriveFolderId))
            {
                return;
            }

            var hoSoList = await db.PackageDossier
                .AsNoTracking()
                .Where(x => !x.Deleted && x.SessionId == sessionId
                    && x.Status != KiemTraConstants.StatusFailed && x.DocumentCount > 0
                    && x.PackageDriveFileId == null)
                .OrderBy(x => x.FileCode)
                .ToListAsync(cancellationToken);

            var filesTheoHoSo = (await db.PackageDocument
                    .AsNoTracking()
                    .Where(x => !x.Deleted && x.SessionId == sessionId
                        && x.Status != KiemTraConstants.StatusFailed)
                    .OrderBy(x => x.Id)
                    .ToListAsync(cancellationToken))
                .ToLookup(x => x.DossierId);

            var schemaHoSo = await schemaService.GetAsync(phien.PackageType, phien.ObjectType, cancellationToken);

            var schemaTaiLieu = new List<PackageSchemaDto>();
            foreach (var loai in MetadataObjectTypeGroups.Doc(phien.DocumentTypes))
            {
                schemaTaiLieu.Add(await schemaService.GetAsync(phien.PackageType, loai, cancellationToken));
            }

            var schemaEntries = schemaTemplate.Load();
            var boNhoExcel = new Dictionary<string, ExcelDaDocDto>(StringComparer.Ordinal);
            var viec = new List<PackageDossier>();
            var soBoQua = 0;

            foreach (var hoSo in hoSoList)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!filesTheoHoSo[hoSo.Id].Any() || string.IsNullOrWhiteSpace(hoSo.FileCode)
                    || string.IsNullOrWhiteSpace(hoSo.ExcelObjectKey))
                {
                    soBoQua++;
                    continue;
                }

                var capDto = await builder.LaySheetAsync(boNhoExcel, hoSo.ExcelObjectKey, schemaHoSo,
                    schemaTaiLieu, cancellationToken);

                if (capDto.TaiLieu.Count == 0)
                {
                    soBoQua++;
                    continue;
                }

                viec.Add(hoSo);
            }

            await CongTienDoAsync(db, sessionId, soBoQua);

            await Parallel.ForEachAsync(viec,
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = KiemTraConstants.ParallelPackageDossiers,
                    CancellationToken = cancellationToken,
                },
                async (hoSo, token) => await DongMotHoSoAsync(phien, dto, hoSo, filesTheoHoSo[hoSo.Id].ToList(),
                    boNhoExcel[hoSo.ExcelObjectKey!], schemaHoSo, schemaTaiLieu, schemaEntries, token));

            await ChotDongGoiAsync(db, sessionId);

            try
            {
                await storage.RemoveSessionAsync(sessionId, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"{nameof(ChayAsync)} khong don duoc kho tam sessionId={sessionId}");
            }
        }

        public async Task DongMotHoSoAsync(PackageSession phien, DongGoiDto dto, PackageDossier hoSo,
            List<PackageDocument> cuaHoSo, ExcelDaDocDto capDto, PackageSchemaDto schemaHoSo,
            IReadOnlyList<PackageSchemaDto> schemaTaiLieu, IReadOnlyList<PackEntryDto> schemaEntries,
            CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<KssmDbContext>();
            var builder = scope.ServiceProvider.GetRequiredService<IPackageBuilderService>();

            await builder.DungMotGoiAsync(phien, phien.DriveFolderId!, dto, hoSo, cuaHoSo, capDto, schemaHoSo,
                schemaTaiLieu, schemaEntries, cancellationToken);

            var now = DateTimeConstants.VietnamNow;

            await db.PackageDossier
                .Where(x => x.Id == hoSo.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.PackageObjid, hoSo.PackageObjid)
                    .SetProperty(x => x.PackageDriveFileId, hoSo.PackageDriveFileId)
                    .SetProperty(x => x.PackageSize, hoSo.PackageSize)
                    .SetProperty(x => x.ModifiedDate, now), CancellationToken.None);

            await CongTienDoAsync(db, phien.Id, 1);
        }

        public async Task CongTienDoAsync(KssmDbContext db, int sessionId, int soLuong)
        {
            if (soLuong <= 0)
            {
                return;
            }

            var now = DateTimeConstants.VietnamNow;

            await db.PackageSession
                .Where(x => x.Id == sessionId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.PackageDone, x => x.PackageDone + soLuong)
                    .SetProperty(x => x.ModifiedDate, now), CancellationToken.None);
        }

        public async Task ChotDongGoiAsync(KssmDbContext db, int sessionId)
        {
            var now = DateTimeConstants.VietnamNow;

            await db.PackageSession
                .Where(x => x.Id == sessionId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(x => x.PackagedDate, (DateTime?)now)
                    .SetProperty(x => x.PackageStatus, KiemTraConstants.PackageStatusDone)
                    .SetProperty(x => x.ModifiedDate, now), CancellationToken.None);
        }

        public async Task GhiLoiAsync(int sessionId, string lyDo)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<KssmDbContext>();
                var now = DateTimeConstants.VietnamNow;

                await db.PackageSession
                    .Where(x => x.Id == sessionId)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(x => x.PackageStatus, KiemTraConstants.PackageStatusFailed)
                        .SetProperty(x => x.PackageFailReason, lyDo)
                        .SetProperty(x => x.ModifiedDate, now));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"{nameof(GhiLoiAsync)} sessionId={sessionId}");
            }
        }
    }
}
