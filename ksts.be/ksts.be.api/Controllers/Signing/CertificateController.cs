using ksts.be.api.Controllers.Base;
using ksts.be.applications.Signing.Dtos;
using ksts.be.applications.Signing.Interfaces;
using ksts.be.shared.Requests;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ksts.be.api.Controllers.Signing
{
    /// <summary>
    /// Chứng thư số dùng để ký.
    ///
    /// Nguồn chứng thư giai đoạn này là cert store của MÁY CHẠY API - xem
    /// .claude/docs/ky-so-web-vs-desktop.md về giới hạn đó và hướng chuyển sang agent ở máy client.
    /// </summary>
    [Route("api/core/chung-thu-so")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class CertificateController : BaseController
    {
        private readonly ICertificateService _certificateService;

        public CertificateController(
            ICertificateService certificateService,
            ILogger<CertificateController> logger) : base(logger)
        {
            _certificateService = certificateService;
        }

        /// <summary>Danh sách chứng thư số. onlySignable=true để chỉ lấy cert ký được.</summary>
        [HttpGet]
        public ApiResponse GetCertificates([FromQuery] SignCertQueryDto dto)
        {
            try
            {
                var result = _certificateService.GetCertificates(dto);
                return new(result);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        /// <summary>Chẩn đoán việc quét kho chứng thư - dùng khi danh sách rỗng mà không rõ nguyên nhân.</summary>
        [HttpGet("chan-doan")]
        public ApiResponse GetDiagnostics()
        {
            try
            {
                var result = _certificateService.GetDiagnostics();
                return new(result);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        /// <summary>Chọn một chứng thư số: thẩm định lại tại thời điểm chọn rồi trả về chi tiết.</summary>
        [HttpPost("chon")]
        public ApiResponse SelectCertificate([FromBody] SelectCertDto dto)
        {
            try
            {
                var result = _certificateService.SelectCertificate(dto);
                return new(result);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }
    }
}
