using ksts.be.api.Controllers.Base;
using ksts.be.applications.LoKy.Dtos;
using ksts.be.applications.LoKy.Interfaces;
using ksts.be.shared.Constants.LoKy;
using ksts.be.shared.Requests;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ksts.be.api.Controllers.LoKy
{
    /// <summary>Ký số hàng loạt: mở lô, nhận file theo đợt, chạy ký nền, theo dõi tiến độ, tải kết quả.</summary>
    [Route("api/core/lo-ky")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class LoKyController : BaseController
    {
        private readonly ILoKyService _loKyService;
        private readonly ILogger<LoKyController> _logger;

        public LoKyController(ILoKyService loKyService, ILogger<LoKyController> logger) : base(logger)
        {
            _loKyService = loKyService;
            _logger = logger;
        }

        /// <summary>Mở một lô rỗng để bắt đầu đẩy file lên.</summary>
        [HttpPost]
        public async Task<ApiResponse> TaoLo([FromBody] TaoLoKyDto input)
        {
            try
            {
                return new(await _loKyService.TaoLoAsync(input));
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        /// <summary>Nhận một đợt file PDF vào lô. Gọi lại nhiều lần cho tới hết thư mục.</summary>
        [HttpPost("{id}/them-file")]
        public async Task<ApiResponse> ThemFile(int id)
        {
            try
            {
                return new(await _loKyService.ThemFileAsync(id, Request.Form.Files));
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        /// <summary>
        /// Nhận file từ một thư mục CÓ SẴN trên kho object. Bỏ hẳn khâu tải lên: lô chỉ ghi lại object key
        /// đang có nên file không bị chép đi đâu cả.
        /// </summary>
        [HttpPost("{id}/them-tu-kho")]
        public async Task<ApiResponse> ThemTuKho(int id, [FromBody] ThemFileTuKhoDto input)
        {
            try
            {
                return new(await _loKyService.ThemFileTuKhoAsync(id, input));
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        /// <summary>
        /// Đẩy bản đã ký lên thư mục dùng chung của kho, chạy nền. Là lựa chọn SONG SONG với tải zip về.
        /// </summary>
        [HttpPost("{id}/day-len-kho")]
        public async Task<ApiResponse> DayLenKho(int id)
        {
            try
            {
                return new(await _loKyService.BatDauDayLenKhoAsync(id));
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        /// <summary>Bắt đầu ký cả lô bằng chứng thư đã chọn.</summary>
        [HttpPost("{id}/bat-dau")]
        public async Task<ApiResponse> BatDau(int id, [FromBody] BatDauKyDto input)
        {
            try
            {
                return new(await _loKyService.BatDauAsync(id, input));
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        /// <summary>Tiến độ và danh sách file của lô.</summary>
        [HttpGet("{id}/trang-thai")]
        public async Task<ApiResponse> TrangThai(int id)
        {
            try
            {
                return new(await _loKyService.TrangThaiAsync(id));
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        /// <summary>Lô còn dở của người đang đăng nhập, để mở lại màn hình là thấy đúng tiến độ.</summary>
        [HttpGet("dang-chay")]
        public async Task<ApiResponse> LoDangChay()
        {
            try
            {
                return new(await _loKyService.LoDangChayAsync());
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        /// <summary>Dừng lô đang ký. File đã ký xong giữ nguyên và vẫn hợp lệ.</summary>
        [HttpPost("{id}/huy")]
        public async Task<ApiResponse> Huy(int id)
        {
            try
            {
                await _loKyService.HuyAsync(id);
                return new();
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        /// <summary>
        /// File nén chứa toàn bộ bản đã ký. KHÔNG bọc ApiResponse: trình duyệt tải thẳng xuống đĩa, gói lô vài
        /// GB vào envelope JSON rồi mới lưu là hết bộ nhớ trang.
        ///
        /// KHÔNG đòi Bearer vì trình duyệt điều hướng tới đây thì không gắn được header Authorization; chặn
        /// bằng token phát riêng cho lô, kiểm trong service.
        /// </summary>
        [HttpGet("{id}/zip")]
        [AllowAnonymous]
        public async Task<IActionResult> TaiZip(int id, string token)
        {
            try
            {
                var stream = await _loKyService.TaiZipAsync(id, token);
                return File(stream, LoKyConstants.ZipContentType,
                    $"lo-ky-{id}-{DateTime.UtcNow:yyyyMMddHHmmss}.zip");
            }
            catch (Exception ex)
            {
                // Trả 404 trơn chứ không lộ lý do: đường này mở cho request chưa đăng nhập, phân biệt "sai
                // token" với "lô chưa xong" là chỉ điểm cho người dò. Lý do thật ghi vào log.
                _logger.LogWarning(ex, "Tải zip lô {LoKyId} thất bại", id);
                return NotFound();
            }
        }
    }
}
