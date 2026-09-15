using kssm.be.api.Controllers.Base;
using kssm.be.applications.DongGoi.Config.Dtos;
using kssm.be.applications.DongGoi.Config.Interfaces;
using kssm.be.shared.Constants.DongGoi;
using kssm.be.shared.Requests;
using Microsoft.AspNetCore.Mvc;

namespace kssm.be.api.Controllers.DongGoi
{
    [Route("api/core/dong-goi/cau-hinh")]
    [ApiController]
    public class ConfigController : BaseController
    {
        private readonly IConfigService _configService;

        public ConfigController(
            IConfigService configService,
            ILogger<ConfigController> logger) : base(logger)
        {
            _configService = configService;
        }

        [HttpGet("find-paging")]
        public async Task<ApiResponse> FindPaging([FromQuery] FindPagingPackageFieldDto dto)
        {
            try
            {
                var result = await _configService.FindPagingAsync(dto);
                return new(result);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }



        [HttpGet("drop-down")]
        public async Task<ApiResponse> GetDropDownObjectType()
        {
            try
            {
                var result = await _configService.GetDropDownTypeTT05Async();
                return new(result);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpGet("xuat-excel")]
        public async Task<IActionResult> ExportExcel([FromQuery] ExportPackageFieldDto dto)
        {
            try
            {
                var result = await _configService.ExportExcelAsync(dto);
                return File(result.Content, XuatExcelConstants.ContentType, result.FileName);
            }
            catch (Exception ex)
            {
                return Ok(OkException(ex));
            }
        }
    }
}
