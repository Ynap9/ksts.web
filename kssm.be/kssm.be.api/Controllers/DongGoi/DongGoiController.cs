using kssm.be.api.Controllers.Base;
using kssm.be.applications.DongGoi.Package.Dtos;
using kssm.be.applications.DongGoi.Package.Interfaces;
using kssm.be.shared.Constants.DongGoi;
using kssm.be.shared.Requests;
using Microsoft.AspNetCore.Mvc;

namespace kssm.be.api.Controllers.DongGoi
{
    [Route("api/core/dong-goi/goi")]
    [ApiController]
    public class DongGoiController : BaseController
    {
        private readonly IPackageBuilderService _packageBuilderService;

        public DongGoiController(
            IPackageBuilderService packageBuilderService,
            ILogger<DongGoiController> logger) : base(logger)
        {
            _packageBuilderService = packageBuilderService;
        }

        [HttpPost("{id}/dong-goi")]
        public async Task<ApiResponse> DongGoi(int id, [FromBody] DongGoiDto dto,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await _packageBuilderService.DongGoiAsync(id, dto, cancellationToken);
                return new(result);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpGet("{id}/danh-sach")]
        public async Task<ApiResponse> DanhSach(int id, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _packageBuilderService.DanhSachGoiAsync(id, cancellationToken);
                return new(result);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpGet("{id}/ho-so/{hoSoId}/tai-ve")]
        public async Task<IActionResult> TaiVe(int id, int hoSoId, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _packageBuilderService.TaiGoiAsync(id, hoSoId, cancellationToken);
                return File(result.NoiDung, SipPackConstants.MimeTypeZip, result.TenFile);
            }
            catch (Exception ex)
            {
                return Ok(OkException(ex));
            }
        }
    }
}
