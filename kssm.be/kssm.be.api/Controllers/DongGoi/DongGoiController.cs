using kssm.be.api.Controllers.Base;
using kssm.be.applications.DongGoi.Package.Dtos;
using kssm.be.applications.DongGoi.Package.Interfaces;
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

    }
}
