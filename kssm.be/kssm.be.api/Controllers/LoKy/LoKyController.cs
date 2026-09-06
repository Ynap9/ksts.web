using kssm.be.api.Controllers.Base;
using kssm.be.applications.LoKy.Dtos;
using kssm.be.applications.LoKy.Interfaces;
using kssm.be.shared.Constants.LoKy;
using kssm.be.shared.Requests;
using Microsoft.AspNetCore.Mvc;

namespace kssm.be.api.Controllers.LoKy
{
    /// <summary>
    /// Lô ký số hàng loạt. Bên gọi đứng trước service này đã xác thực người dùng nên ở đây không kiểm token;
    /// riêng đường tải zip còn có thêm token của chính lô vì trình duyệt điều hướng thẳng tới đó.
    /// </summary>
    [ApiController]
    [Route("api/core/lo-ky")]
    public class LoKyController : BaseController
    {
        private readonly ILoKyService _loKyService;
        private readonly ILogger<LoKyController> _logger;

        public LoKyController(ILoKyService loKyService, ILogger<LoKyController> logger) : base(logger)
        {
            _loKyService = loKyService;
            _logger = logger;
        }

        /// <summary>Dự án ký được — chỉ dự án đang nghiệm thu.</summary>
        [HttpGet("du-an")]
        public async Task<ApiResponse> DanhSachDuAn(CancellationToken cancellationToken)
        {
            try
            {
                var result = await _loKyService.DanhSachDuAnAsync(cancellationToken);
                return new(result);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        /// <summary>Mở lô. Có projectId thì lô nạp luôn file của dự án; không có thì chờ file tải lên.</summary>
        [HttpPost]
        public async Task<ApiResponse> TaoLo([FromBody] TaoLoKyDto input, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _loKyService.TaoLoAsync(input, cancellationToken);
                return new(result);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        /// <summary>Nhận một đợt file tải lên. Chỉ dùng cho lô không gắn dự án.</summary>
        [HttpPost("{id}/them-file")]
        public async Task<ApiResponse> ThemFile(int id, CancellationToken cancellationToken)
        {
            try
            {
                var files = Request.Form.Files;
                var result = await _loKyService.ThemFileAsync(id, files, cancellationToken);
                return new(result);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        /// <summary>Nhận chứng thư công khai của phiên ký rồi mở phiên. Gọi TRƯỚC khi bắt đầu ký.</summary>
        [HttpPost("{id}/mo-phien")]
        public async Task<ApiResponse> MoPhien(int id, [FromBody] MoPhienKyDto input)
        {
            try
            {
                var result = await _loKyService.MoPhienKyAsync(id, input);
                return new(result);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        [HttpPost("{id}/dong-phien")]
        public async Task<ApiResponse> DongPhien(int id)
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

        /// <summary>Bắt đầu ký, hoặc ký tiếp sau khi lô tạm dừng.</summary>
        [HttpPost("{id}/bat-dau")]
        public async Task<ApiResponse> BatDau(int id, [FromBody] BatDauKyDto input)
        {
            try
            {
                var result = await _loKyService.BatDauAsync(id, input);
                return new(result);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        /// <summary>
        /// Lấy các yêu cầu ký đang chờ. Lời gọi này bị GIỮ tới khi có việc hoặc hết hạn chờ — bên gọi không
        /// được đặt timeout ngắn hơn, và gọi lại ngay khi nó trả về.
        /// </summary>
        [HttpGet("{id}/cho-ky")]
        public async Task<ApiResponse> ChoKy(int id, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _loKyService.LayYeuCauKyAsync(id, cancellationToken);
                return new(result);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        /// <summary>Nộp chữ ký của cả một đợt. Kết quả báo phiên đã mất sẽ đưa lô về trạng thái tạm dừng.</summary>
        [HttpPost("{id}/chu-ky")]
        public async Task<ApiResponse> ChuKy(int id, [FromBody] List<NopChuKyDto> input)
        {
            try
            {
                await _loKyService.NopChuKyAsync(id, input);
                return new(true);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        /// <summary>Tạm dừng lô. File đang ký trả về hàng đợi, không bị tính là lỗi.</summary>
        [HttpPost("{id}/tam-dung")]
        public async Task<ApiResponse> TamDung(int id, [FromBody] TamDungKyDto input)
        {
            try
            {
                var result = await _loKyService.TamDungAsync(id, input);
                return new(result);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        /// <summary>Đóng lô hẳn. Muốn ký nữa phải mở lô mới.</summary>
        [HttpPost("{id}/huy")]
        public async Task<ApiResponse> Huy(int id)
        {
            try
            {
                var result = await _loKyService.HuyAsync(id);
                return new(result);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        /// <summary>Danh sách đầy đủ các file của lô, lấy một lần lúc mở màn hình.</summary>
        [HttpGet("{id}/danh-sach-file")]
        public async Task<ApiResponse> DanhSachFile(int id)
        {
            try
            {
                var result = await _loKyService.DanhSachFileAsync(id);
                return new(result);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        /// <summary>Tiến độ để hỏi theo nhịp: bộ đếm, file lỗi, file vừa xong và cờ phiên ký còn sống.</summary>
        [HttpGet("{id}/trang-thai")]
        public async Task<ApiResponse> TrangThai(int id)
        {
            try
            {
                var result = await _loKyService.TrangThaiAsync(id);
                return new(result);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        /// <summary>Lô đang chạy dở của một người, để mở lại màn hình thấy đúng lô cũ.</summary>
        [HttpGet("dang-chay")]
        public async Task<ApiResponse> LoDangChay([FromQuery] string? nguoiTaoId)
        {
            try
            {
                var result = await _loKyService.LoDangChayAsync(nguoiTaoId);
                return new(result);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        /// <summary>
        /// Tải các bản đã ký dưới dạng zip. Trả bytes thô, KHÔNG bọc envelope — trình duyệt điều hướng thẳng
        /// tới đây nên mọi trường hợp sai đều trả 404 trơn, không nêu lý do cho người dò.
        /// </summary>
        [HttpGet("{id}/zip")]
        public async Task<IActionResult> TaiZip(int id, [FromQuery] string token,
            CancellationToken cancellationToken)
        {
            Response.ContentType = LoKyConstants.ZipContentType;
            Response.Headers.ContentDisposition =
                $"attachment; filename=\"ky-so-{DateTime.UtcNow:yyyyMMddHHmmss}.zip\"";

            try
            {
                await _loKyService.GhiNenAsync(id, token, Response.Body, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Tải zip lô {LoKyId} thất bại", id);

                // Header đã gửi đi rồi thì không đổi sang response lỗi được nữa, chỉ còn cách cắt luồng.
                if (Response.HasStarted)
                {
                    return new EmptyResult();
                }

                return NotFound();
            }

            return new EmptyResult();
        }
    }
}
