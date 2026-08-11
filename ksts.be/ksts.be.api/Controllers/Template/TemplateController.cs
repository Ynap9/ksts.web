using ksts.be.api.Controllers.Base;
using ksts.be.applications.Template.Dtos;
using ksts.be.applications.Template.Interfaces;
using ksts.be.shared.Requests;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ksts.be.api.Controllers.Template
{
    /// <summary>
    /// Template cấu hình chữ ký - bộ cấu hình dựng sẵn gồm chứng thư số, lý do/nơi ký, ảnh dấu đỏ, ảnh chữ ký
    /// tươi và toạ độ từng khối. Template thuộc về người dùng đang đăng nhập nên mọi endpoint đều yêu cầu token.
    /// </summary>
    [Route("api/core/template-chu-ky")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class TemplateController : BaseController
    {
        private readonly ITemplateService _templateService;

        public TemplateController(
            ITemplateService templateService,
            ILogger<TemplateController> logger) : base(logger)
        {
            _templateService = templateService;
        }

        /// <summary>Tạo template rỗng, chỉ đặt tên. Phần cấu hình ký đặt sau ở màn chi tiết.</summary>
        [HttpPost]
        public ApiResponse Create([FromBody] AddTemplateDto dto)
        {
            try
            {
                var result = _templateService.Create(dto);
                return new(result);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        /// <summary>Đổi tên template, không đụng tới phần cấu hình ký.</summary>
        [HttpPut]
        public ApiResponse Update([FromBody] UpdateTemplateDto dto)
        {
            try
            {
                var result = _templateService.Update(dto);
                return new(result);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        /// <summary>Đặt cấu hình ký lần đầu. Nhận multipart vì có thể kèm ảnh dấu đỏ / chữ ký tươi.</summary>
        [HttpPost("cau-hinh")]
        public async Task<ApiResponse> CreateConfig([FromForm] AddConfigTemplateDto dto)
        {
            try
            {
                var result = await _templateService.CreateConfigAsync(dto);
                return new(result);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        /// <summary>Ghi đè toàn bộ cấu hình ký, kể cả danh sách toạ độ.</summary>
        [HttpPut("cau-hinh")]
        public async Task<ApiResponse> UpdateConfig([FromForm] UpdateConfigTemplateDto dto)
        {
            try
            {
                var result = await _templateService.UpdateConfigAsync(dto);
                return new(result);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        /// <summary>Xoá template và dọn ảnh của nó trên kho lưu trữ.</summary>
        [HttpDelete("{id:int}")]
        public async Task<ApiResponse> Delete(int id)
        {
            try
            {
                await _templateService.DeleteAsync(id);
                return new();
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        /// <summary>Thông tin file PDF mẫu đi kèm app.</summary>
        [HttpGet("file-mau")]
        public ApiResponse GetSampleFile()
        {
            try
            {
                var result = _templateService.GetSampleFile();
                return new(result);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        /// <summary>
        /// Nội dung file PDF mẫu. Đây là endpoint DUY NHẤT của module không bọc ApiResponse - trình xem PDF ở
        /// FE cần bytes thô.
        /// </summary>
        [HttpGet("file-mau/noi-dung")]
        public IActionResult GetSampleFileContent()
        {
            var stream = _templateService.OpenSampleFile();
            return File(stream, "application/pdf");
        }

        /// <summary>
        /// Vị trí gợi ý cho con dấu và chữ ký tươi: trung điểm đoạn nối chức danh với tên người ký.
        /// Không gửi file PDF thì tính trên file mẫu đi kèm app.
        /// </summary>
        [HttpPost("vi-tri-goi-y")]
        public async Task<ApiResponse> GetSuggestedPlacement([FromForm] SuggestPlacementDto dto)
        {
            try
            {
                var result = await _templateService.GetSuggestedPlacementAsync(dto);
                return new(result);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        /// <summary>Danh sách template có phân trang. pageSize = -1 để lấy hết.</summary>
        [HttpGet("find-paging")]
        public async Task<ApiResponse> FindPaging([FromQuery] FindPagingTemplateDto dto)
        {
            try
            {
                var result = await _templateService.FindPagingAsync(dto);
                return new(result);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        /// <summary>Lấy một template kèm toàn bộ toạ độ.</summary>
        [HttpGet("{id:int}")]
        public async Task<ApiResponse> GetById(int id)
        {
            try
            {
                var result = await _templateService.GetByIdAsync(id);
                return new(result);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }
    }
}
