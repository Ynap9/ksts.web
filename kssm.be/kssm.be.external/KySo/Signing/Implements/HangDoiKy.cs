using kssm.be.external.KySo.Signing.Dtos;
using kssm.be.external.KySo.Signing.Interfaces;
using kssm.be.shared.Constants.Signing;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Security.Cryptography.X509Certificates;

namespace kssm.be.external.KySo.Signing.Implements
{
    public class HangDoiKy : IHangDoiKy
    {
        private readonly ILogger<HangDoiKy> _logger;

        private readonly ConcurrentDictionary<int, PhienKy> _phien = new();

        public HangDoiKy(ILogger<HangDoiKy> logger)
        {
            _logger = logger;
        }

        public void MoPhien(int loKyId, X509Certificate2 cert)
        {
            _logger.LogInformation("{Method} loKyId={LoKyId} thumbprint={Thumbprint}",
                nameof(MoPhien), loKyId, cert.Thumbprint);

            DongPhien(loKyId);
            _phien[loKyId] = new PhienKy(cert);
        }

        public X509Certificate2 LayChungThu(int loKyId)
        {
            return _phien.TryGetValue(loKyId, out var phien)
                ? phien.Cert
                : throw new InvalidOperationException(
                    "Chưa mở phiên ký với máy người dùng, hoặc phiên đã đóng.");
        }

        public bool PhienConSong(int loKyId) => _phien.ContainsKey(loKyId);

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

            await using (quaHan.Token.Register(() => cho.TrySetException(
                new TimeoutException("Không nhận được chữ ký từ máy người dùng."))))
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

        public bool NopKetQua(int loKyId, IEnumerable<KetQuaKyDto> ketQua)
        {
            if (!_phien.TryGetValue(loKyId, out var phien))
            {
                return false;
            }

            var phienDaMat = false;

            foreach (var item in ketQua)
            {
                phienDaMat |= item.PhienDaMat;

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

            // Phiên mất thì buông luôn: mọi lượt còn treo được đánh thức bằng lỗi ngay tại đây, thay vì mỗi
            // lượt phải đợi hết 120 giây rồi mới chết.
            if (phienDaMat)
            {
                _logger.LogWarning("Lô {LoKyId}: máy người dùng báo phiên ký đã mất", loKyId);
                DongPhien(loKyId);
            }

            return phienDaMat;
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
