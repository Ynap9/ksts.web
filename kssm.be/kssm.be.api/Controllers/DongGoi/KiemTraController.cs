using kssm.be.api.Controllers.Base;
using kssm.be.applications.DongGoi.Package.Dtos;
using kssm.be.applications.DongGoi.Package.Interfaces;
using kssm.be.shared.Requests;
using Microsoft.AspNetCore.Mvc;

namespace kssm.be.api.Controllers.DongGoi
{
    [Route("api/core/dong-goi/kiem-tra")]
    [ApiController]
    public class KiemTraController : BaseController
    {
        private const string TruongDuongDan = "duongDan";

        private readonly IPackageSessionService _packageSessionService;

        public KiemTraController(
            IPackageSessionService packageSessionService,
            ILogger<KiemTraController> logger) : base(logger)
        {
            _packageSessionService = packageSessionService;
        }

        [HttpPost("tao-phien")]
        public async Task<ApiResponse> TaoPhien([FromBody] TaoPhienDto dto, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _packageSessionService.TaoPhienAsync(dto, cancellationToken);
                return new(result);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpPost("{id}/them-file")]
        public async Task<ApiResponse> ThemFile(int id, CancellationToken cancellationToken)
        {
            try
            {
                var files = Request.Form.Files;
                var duongDan = Request.Form[TruongDuongDan].Select(x => x ?? string.Empty).ToList();
                var result = await _packageSessionService.ThemFileAsync(id, files, duongDan, cancellationToken);
                return new(result);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpPost("{id}/bat-dau")]
        public async Task<ApiResponse> BatDau(int id, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _packageSessionService.BatDauAsync(id, cancellationToken);
                return new(result);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpGet("{id}/tien-do")]
        public async Task<ApiResponse> TienDo(int id, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _packageSessionService.TienDoAsync(id, cancellationToken);
                return new(result);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpGet("{id}/ket-qua")]
        public async Task<ApiResponse> KetQua(int id, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _packageSessionService.KetQuaAsync(id, cancellationToken);
                return new(result);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpPost("{id}/huy")]
        public async Task<ApiResponse> Huy(int id, CancellationToken cancellationToken)
        {
            try
            {
                await _packageSessionService.HuyAsync(id, cancellationToken);
                return new(true);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }
    }
}
