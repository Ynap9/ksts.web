using ksts.be.api.Controllers.Base;
using ksts.be.applications.LoKy.Dtos;
using ksts.be.applications.LoKy.Interfaces;
using ksts.be.external.Signing.Dtos;
using ksts.be.external.Signing.Interfaces;
using ksts.be.shared.Constants.LoKy;
using ksts.be.shared.Constants.Signing;
using ksts.be.shared.Requests;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
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
        private readonly IHangDoiKy _hangDoiKy;
        private readonly ILogger<LoKyController> _logger;

        public LoKyController(ILoKyService loKyService,
            IHangDoiKy hangDoiKy, ILogger<LoKyController> logger) : base(logger)
        {
            _loKyService = loKyService;
            _hangDoiKy = hangDoiKy;
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

        /// <summary>Nhận một đợt file PDF tải lên từ máy người dùng. Gọi lại nhiều lần cho tới hết thư mục.</summary>
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
        /// Nhận chứng thư phần công khai của phiên ký từ máy người dùng. Gọi trước khi bắt đầu ký.
        /// </summary>
        [HttpPost("{id}/mo-phien")]
        public async Task<ApiResponse> MoPhienKy(int id, [FromBody] MoPhienKyDto input)
        {
            try
            {
                return new(await _loKyService.MoPhienKyAsync(id, input));
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        /// <summary>Đóng phiên ký, huỷ mọi lượt còn treo.</summary>
        [HttpPost("{id}/dong-phien")]
        public async Task<ApiResponse> DongPhienKy(int id)
        {
            try
            {
                await _loKyService.DongPhienKyAsync(id);
                return new(true);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        /// <summary>
        /// Lấy các yêu cầu ký đang chờ. Lời gọi được GIỮ tới khi có việc hoặc hết thời gian chờ, nên trang
        /// web không phải hỏi theo nhịp — độ trễ đó cộng thẳng vào từng file vì token ký tuần tự.
        /// </summary>
        [HttpGet("{id}/cho-ky")]
        public async Task<ApiResponse> ChoKy(int id)
        {
            try
            {
                await _loKyService.LayLoAsync(id);
                var ket = await _hangDoiKy.LayYeuCauAsync(id,
                    TimeSpan.FromSeconds(SigningQueueConstants.GiayChoLayViec), HttpContext.RequestAborted);
                return new(ket);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        /// <summary>Nộp chữ ký của một đợt yêu cầu, đánh thức các lượt ký đang chờ.</summary>
        [HttpPost("{id}/chu-ky")]
        public async Task<ApiResponse> NopChuKy(int id, [FromBody] List<KetQuaKyDto> input)
        {
            try
            {
                await _loKyService.LayLoAsync(id);
                _hangDoiKy.NopKetQua(id, input);
                return new(true);
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

        /// <summary>
        /// Danh sách file của lô, gọi MỘT lần khi mở màn hình. Tiến độ hỏi theo nhịp nên không kèm danh sách
        /// này.
        /// </summary>
        [HttpGet("{id}/danh-sach-file")]
        public async Task<ApiResponse> DanhSachFile(int id)
        {
            try
            {
                return new(await _loKyService.DanhSachFileAsync(id));
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        /// <summary>Tiến độ và danh sách file LỖI của lô.</summary>
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
        /// File nén chứa toàn bộ bản đã ký, dựng NGAY LÚC TẢI bằng cách kéo từng bản từ kho ra rồi nén vào
        /// luồng gửi đi - máy chủ không giữ file nén nào trên đĩa. KHÔNG bọc ApiResponse: trình duyệt tải
        /// thẳng xuống đĩa, gói lô vài GB vào envelope JSON rồi mới lưu là hết bộ nhớ trang.
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
                // ZipArchive ghi header và bảng mục lục bằng lệnh ghi ĐỒNG BỘ, mà Response.Body cấm ghi đồng
                // bộ theo mặc định - không mở cờ này thì hỏng ngay ở entry đầu tiên.
                var dieuKhienThan = HttpContext.Features.Get<IHttpBodyControlFeature>();
                if (dieuKhienThan != null)
                {
                    dieuKhienThan.AllowSynchronousIO = true;
                }

                Response.ContentType = LoKyConstants.ZipContentType;
                Response.Headers.ContentDisposition =
                    $"attachment; filename=\"giay-bao-trung-tuyen-da-ky-so-{DateTime.UtcNow:yyyyMMddHHmmss}.zip\"";

                await _loKyService.GhiNenAsync(id, token, Response.Body, HttpContext.RequestAborted);
                return new EmptyResult();
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
