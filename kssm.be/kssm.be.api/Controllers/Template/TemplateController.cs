using kssm.be.api.Controllers.Base;
using kssm.be.applications.Template.Dtos;
using kssm.be.applications.Template.Interfaces;
using kssm.be.shared.Requests;
using Microsoft.AspNetCore.Mvc;

namespace kssm.be.api.Controllers.Template
{
    /// <summary>
    /// Template cấu hình chữ ký - bộ cấu hình dựng sẵn gồm chứng thư số, lý do/nơi ký, ảnh dấu đỏ, ảnh chữ ký
    /// tươi và toạ độ từng khối.
    ///
    /// Bên gọi là sao_mai_be chứ không phải trình duyệt, và kssm.be đứng sau nó như một API external nên
    /// không có token: người dùng đã được sao_mai_be xác thực từ trước, còn chủ sở hữu template do bảng
    /// template ký số bên MongoDB của nó giữ theo Id mà POST đầu tiên trả về.
    /// </summary>
    [Route("api/core/template-chu-ky")]
    [ApiController]
    public class TemplateController : BaseController
    {
        private readonly ITemplateService _templateService;

        public TemplateController(
            ITemplateService templateService,
            ILogger<TemplateController> logger) : base(logger)
        {
            _templateService = templateService;
        }

        /// <summary>Tạo template rỗng, chỉ đặt tên. Trả về Id để bên gọi lưu lại; cấu hình ký đặt sau.</summary>
        [HttpPost]
        public async Task<ApiResponse> Create([FromBody] AddTemplateDto dto)
        {
            try
            {
                var result = await _templateService.CreateAsync(dto);
                return new(result);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        /// <summary>Đổi tên template, không đụng tới phần cấu hình ký.</summary>
        [HttpPut]
        public async Task<ApiResponse> Update([FromBody] UpdateTemplateDto dto)
        {
            try
            {
                var result = await _templateService.UpdateAsync(dto);
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

        /// <summary>Thông tin file PDF mẫu đi kèm bản cài.</summary>
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
        /// Nội dung file PDF mẫu. Đây là endpoint DUY NHẤT của module không bọc ApiResponse - bên gọi cần
        /// bytes thô để dựng trang.
        /// </summary>
        [HttpGet("file-mau/noi-dung")]
        public IActionResult GetSampleFileContent()
        {
            var stream = _templateService.OpenSampleFile();
            return File(stream, "application/pdf");
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
