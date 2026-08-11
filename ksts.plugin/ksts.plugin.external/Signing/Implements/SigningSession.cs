using ksts.plugin.external.Certificates.Implements;
using ksts.plugin.external.Signing.Dtos;
using ksts.plugin.external.Signing.Interfaces;
using ksts.plugin.shared.Constants;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace ksts.plugin.external.Signing.Implements
{
    /// <inheritdoc/>
    public class SigningSession : ISigningSession
    {
        private static readonly StoreLocation[] Locations =
        {
            StoreLocation.CurrentUser,
            StoreLocation.LocalMachine,
        };

        private readonly ILogger<SigningSession> _logger;

        /// <summary>Một máy chỉ có một người ngồi ký, nên phiên là duy nhất và mọi lượt ký xếp hàng qua khoá này.</summary>
        private readonly object _khoa = new();

        private X509Certificate2? _cert;
        private DateTime _lanDungCuoi;

        public SigningSession(ILogger<SigningSession> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc/>
        public MoPhienKetQuaDto MoPhien(string thumbprint)
        {
            _logger.LogInformation("{Method} thumbprint={Thumbprint}", nameof(MoPhien), thumbprint);

            lock (_khoa)
            {
                DongPhienTrongKhoa();

                var cert = TimChungThu(thumbprint)
                    ?? throw new InvalidOperationException(
                        "Không tìm thấy chứng thư số theo vân tay đã chọn. Kiểm tra token đã cắm chưa.");

                // Ký thử một mẩu ngẫu nhiên để BUỘC middleware mở khoá ngay bây giờ: hộp PIN bật đúng ở đây,
                // thay vì bật giữa chừng khi lô đã chạy được vài trăm file.
                var probe = RandomNumberGenerator.GetBytes(ChungThuSoConstants.PreflightTestDataSize);
                KyBangCert(cert, probe);

                _cert = cert;
                _lanDungCuoi = DateTime.UtcNow;

                return new MoPhienKetQuaDto
                {
                    Thumbprint = cert.Thumbprint,
                    CommonName = cert.GetNameInfo(X509NameType.SimpleName, false),
                    ChungThuBase64 = Convert.ToBase64String(cert.RawData),
                };
            }
        }

        /// <inheritdoc/>
        public byte[] Ky(byte[] duLieu)
        {
            lock (_khoa)
            {
                if (_cert != null && DateTime.UtcNow - _lanDungCuoi > TimeSpan.FromMinutes(KySoConstants.PhutTuDongDongPhien))
                {
                    _logger.LogInformation("Phiên ký quá hạn không dùng, tự đóng");
                    DongPhienTrongKhoa();
                }

                if (_cert == null)
                {
                    throw new InvalidOperationException("Chưa mở phiên ký, hoặc phiên đã đóng vì để lâu không dùng.");
                }

                var chuKy = KyBangCert(_cert, duLieu);
                _lanDungCuoi = DateTime.UtcNow;
                return chuKy;
            }
        }

        /// <inheritdoc/>
        public void DongPhien()
        {
            lock (_khoa)
            {
                DongPhienTrongKhoa();
            }
        }

        /// <summary>
        /// Giải phóng handle khoá. Chỉ gọi khi đã giữ khoá - phiên là tài nguyên dùng chung của cả tiến trình.
        /// </summary>
        public void DongPhienTrongKhoa()
        {
            _cert?.Dispose();
            _cert = null;
        }

        /// <summary>Tìm chứng thư kèm khoá riêng theo vân tay, quét cả kho của người dùng lẫn của máy.</summary>
        public X509Certificate2? TimChungThu(string thumbprint)
        {
            foreach (var location in Locations)
            {
                try
                {
                    using var store = new X509Store(StoreName.My, location);
                    store.Open(OpenFlags.ReadOnly);

                    var found = store.Certificates
                        .Find(X509FindType.FindByThumbprint, thumbprint, false)
                        .FirstOrDefault(x => x.HasPrivateKey);

                    if (found != null)
                    {
                        return found;
                    }
                }
                catch (CryptographicException ex)
                {
                    // Kho của máy thường không mở được khi chạy quyền người dùng thường - bỏ qua kho đó.
                    _logger.LogWarning(ex, "Không mở được kho chứng thư {Location}", location);
                }
            }

            return null;
        }

        /// <summary>
        /// Ký SHA-256 bằng khoá riêng của chứng thư. Nhận cả RSA lẫn ECDSA vì chứng thư của các nhà cung cấp
        /// trong nước dùng cả hai; thuật toán phải khớp với bên máy chủ lắp CMS.
        /// </summary>
        public byte[] KyBangCert(X509Certificate2 cert, byte[] duLieu)
        {
            using var rsa = cert.GetRSAPrivateKey();
            if (rsa != null)
            {
                return rsa.SignData(duLieu, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }

            using var ecdsa = cert.GetECDsaPrivateKey()
                ?? throw new InvalidOperationException("Chứng thư số không dùng thuật toán RSA hoặc ECDSA.");

            return ecdsa.SignData(duLieu, HashAlgorithmName.SHA256);
        }
    }
}
