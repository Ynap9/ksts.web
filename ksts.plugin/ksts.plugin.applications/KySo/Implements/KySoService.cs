using ksts.plugin.applications.KySo.Dtos;
using ksts.plugin.applications.KySo.Interfaces;
using ksts.plugin.external.Signing.Dtos;
using ksts.plugin.external.Signing.Interfaces;
using Microsoft.Extensions.Logging;

namespace ksts.plugin.applications.KySo.Implements
{
    /// <inheritdoc/>
    public class KySoService : IKySoService
    {
        private readonly ISigningSession _signingSession;
        private readonly ILogger<KySoService> _logger;

        public KySoService(ISigningSession signingSession, ILogger<KySoService> logger)
        {
            _signingSession = signingSession;
            _logger = logger;
        }

        /// <inheritdoc/>
        public MoPhienKetQuaDto MoPhien(MoPhienDto input) => _signingSession.MoPhien(input.Thumbprint);

        /// <inheritdoc/>
        public List<KetQuaKyDto> Ky(KyLoDto input)
        {
            _logger.LogInformation("{Method} soYeuCau={SoYeuCau}", nameof(Ky), input.YeuCau.Count);

            var ketQua = new List<KetQuaKyDto>(input.YeuCau.Count);

            foreach (var yeuCau in input.YeuCau)
            {
                try
                {
                    var chuKy = _signingSession.Ky(Convert.FromBase64String(yeuCau.DuLieuBase64));
                    ketQua.Add(new KetQuaKyDto
                    {
                        YeuCauId = yeuCau.YeuCauId,
                        ChuKyBase64 = Convert.ToBase64String(chuKy),
                    });
                }
                catch (Exception ex)
                {
                    // Ghi lý do chứ KHÔNG ghi dữ liệu đem ký: nhật ký của plugin không được chứa nội dung.
                    _logger.LogWarning(ex, "Ký yêu cầu {YeuCauId} thất bại", yeuCau.YeuCauId);
                    ketQua.Add(new KetQuaKyDto { YeuCauId = yeuCau.YeuCauId, Loi = ex.Message });
                }
            }

            return ketQua;
        }

        /// <inheritdoc/>
        public void DongPhien() => _signingSession.DongPhien();
    }
}
