using ksts.be.external.Signing.Dtos;
using ksts.be.external.Signing.Interfaces;
using ksts.be.shared.Constants.Signing;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Security.Cryptography.X509Certificates;

namespace ksts.be.external.Signing.Implements
{
    /// <inheritdoc/>
    public class HangDoiKy : IHangDoiKy
    {
        private readonly ILogger<HangDoiKy> _logger;

        private readonly ConcurrentDictionary<int, PhienKy> _phien = new();

        public HangDoiKy(ILogger<HangDoiKy> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc/>
        public void MoPhien(int loKyId, X509Certificate2 cert)
        {
            _logger.LogInformation("{Method} loKyId={LoKyId} thumbprint={Thumbprint}",
                nameof(MoPhien), loKyId, cert.Thumbprint);

            DongPhien(loKyId);
            _phien[loKyId] = new PhienKy(cert);
        }

        /// <inheritdoc/>
        public X509Certificate2 LayChungThu(int loKyId)
        {
            return _phien.TryGetValue(loKyId, out var phien)
                ? phien.Cert
                : throw new InvalidOperationException(
                    "Chưa mở phiên ký với máy người dùng, hoặc phiên đã đóng.");
        }

        /// <inheritdoc/>
        public void DongPhien(int loKyId)
        {
            if (!_phien.TryRemove(loKyId, out var phien))
            {
                return;
            }

            // Huỷ mọi lượt đang chờ TRƯỚC khi buông phiên: bỏ mặc là các luồng ký ngồi chờ tới hết hạn.
            foreach (var yeuCau in phien.DangCho.Values)
            {
                yeuCau.TrySetException(new InvalidOperationException("Phiên ký đã đóng."));
            }

            phien.Dispose();
        }

        /// <inheritdoc/>
        public async Task<byte[]> XinChuKyAsync(int loKyId, byte[] duLieu, CancellationToken cancellationToken)
        {
            if (!_phien.TryGetValue(loKyId, out var phien))
            {
                throw new InvalidOperationException("Chưa mở phiên ký với máy người dùng.");
            }

            var yeuCauId = Guid.NewGuid().ToString("N");
            var cho = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
            phien.DangCho[yeuCauId] = cho;
            phien.ChuaGiao.Enqueue(new YeuCauKyDto
            {
                YeuCauId = yeuCauId,
                DuLieuBase64 = Convert.ToBase64String(duLieu),
            });
            phien.CoViec.Release();

            using var quaHan = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            quaHan.CancelAfter(TimeSpan.FromSeconds(SigningQueueConstants.GiaySongCuaYeuCau));

            // Phân biệt HAI lý do cùng làm token này bật: lô bị dừng, và lượt ký quá hạn. Token của lô là
            // token cha nên cả hai đều đi qua đây; gộp chúng lại thành TimeoutException là file đang chờ chữ
            // ký bị ghi LỖI thay vì được trả về hàng đợi, và đúng ngần ấy file không bao giờ ký tiếp được.
            await using (quaHan.Token.Register(() =>
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    cho.TrySetCanceled(cancellationToken);
                    return;
                }

                cho.TrySetException(new TimeoutException("Không nhận được chữ ký từ máy người dùng."));
            }))
            {
                try
                {
                    return await cho.Task;
                }
                finally
                {
                    phien.DangCho.TryRemove(yeuCauId, out _);
                }
            }
        }

        /// <inheritdoc/>
        public async Task<List<YeuCauKyDto>> LayYeuCauAsync(int loKyId, TimeSpan cho,
            CancellationToken cancellationToken)
        {
            if (!_phien.TryGetValue(loKyId, out var phien))
            {
                throw new InvalidOperationException("Chưa mở phiên ký với máy người dùng.");
            }

            // Chờ có việc đầu tiên rồi mới vét: không chờ thì lời gọi trả rỗng liên tục, mà chờ xong mới vét
            // thì gom được luôn những yêu cầu vừa xếp hàng ngay sau nó.
            if (await phien.CoViec.WaitAsync(cho, cancellationToken))
            {
                phien.ChuaGiao.TryDequeue(out var dauTien);
                var ket = new List<YeuCauKyDto>();
                if (dauTien != null)
                {
                    ket.Add(dauTien);
                }

                while (ket.Count < SigningQueueConstants.SoYeuCauMoiDot
                    && phien.CoViec.Wait(0)
                    && phien.ChuaGiao.TryDequeue(out var them))
                {
                    ket.Add(them);
                }

                return ket;
            }

            return [];
        }

        /// <inheritdoc/>
        public void NopKetQua(int loKyId, IEnumerable<KetQuaKyDto> ketQua)
        {
            if (!_phien.TryGetValue(loKyId, out var phien))
            {
                return;
            }

            foreach (var item in ketQua)
            {
                if (!phien.DangCho.TryGetValue(item.YeuCauId, out var cho))
                {
                    continue;
                }

                if (!string.IsNullOrEmpty(item.ChuKyBase64))
                {
                    cho.TrySetResult(Convert.FromBase64String(item.ChuKyBase64));
                }
                else
                {
                    cho.TrySetException(new InvalidOperationException(
                        item.Loi ?? "Máy người dùng không ký được dữ liệu này."));
                }
            }
        }

        /// <summary>Trạng thái một phiên ký: chứng thư công khai, hàng đợi chưa giao và các lượt đang chờ.</summary>
        private sealed class PhienKy(X509Certificate2 cert) : IDisposable
        {
            public X509Certificate2 Cert { get; } = cert;

            public ConcurrentQueue<YeuCauKyDto> ChuaGiao { get; } = new();

            public ConcurrentDictionary<string, TaskCompletionSource<byte[]>> DangCho { get; } = new();

            public SemaphoreSlim CoViec { get; } = new(0);

            public void Dispose()
            {
                CoViec.Dispose();
                Cert.Dispose();
            }
        }
    }
}
