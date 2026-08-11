using ksts.be.api.Controllers.Base;
using ksts.be.applications.GiayBao.Interfaces;
using ksts.be.shared.Constants.GiayBao;
using ksts.be.external.Jobs.Interfaces;
using ksts.be.shared.Request.AppException;
using ksts.be.shared.Requests;
using ksts.be.shared.Requests.ErrorRequest;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ksts.be.api.Controllers.GiayBao
{
    /// <summary>In giấy báo trúng tuyển hàng loạt từ danh sách thí sinh trong file Excel.</summary>
    [Route("api/core/giay-bao")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class GiayBaoController : BaseController
    {
        private readonly IGiayBaoService _giayBaoService;
        private readonly IZipJobStore _zipJobStore;

        public GiayBaoController(
            IGiayBaoService giayBaoService,
            IZipJobStore zipJobStore,
            ILogger<GiayBaoController> logger) : base(logger)
        {
            _giayBaoService = giayBaoService;
            _zipJobStore = zipJobStore;
        }

        /// <summary>Danh sách sheet trong file Excel để người dùng chọn sheet chứa thí sinh.</summary>
        [HttpPost("danh-sach-sheet")]
        public ApiResponse DanhSachSheet(IFormFile file)
        {
            try
            {
                var result = _giayBaoService.DanhSachSheet(file);
                return new(result);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        /// <summary>Danh sách thí sinh trong sheet đã chọn để hiển thị trước khi in.</summary>
        [HttpPost("danh-sach-thi-sinh")]
        public ApiResponse DanhSachThiSinh(IFormFile file, string? sheetName, int startRow = 1)
        {
            try
            {
                var result = _giayBaoService.DanhSachThiSinh(file, sheetName, startRow);
                return new(result);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        /// <summary>Mở lô dựng giấy báo chạy nền, trả về JobId để hỏi tiến độ.</summary>
        [HttpPost("tao-zip")]
        public ApiResponse TaoZip(IFormFile file, string? sheetName, int startRow = 1)
        {
            try
            {
                var result = _giayBaoService.BatDauTaoZip(file, sheetName, startRow);
                return new(result);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        /// <summary>Tiến độ của lô đang dựng.</summary>
        [HttpGet("tao-zip/{jobId}")]
        public ApiResponse TienDo(string jobId)
        {
            try
            {
                var job = _zipJobStore.Lay(jobId)
                    ?? throw new UserFriendlyException(ErrorCodes.NotFound, "Lô dựng giấy báo không còn tồn tại.");
                return new(job);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        /// <summary>
        /// Mở việc đẩy cả lô lên kho object chạy nền. Là lựa chọn SONG SONG với tải zip về, không thay thế:
        /// lô dựng xong rồi thì đẩy lên kho hay tải về máy đều được, làm cả hai cũng được.
        /// </summary>
        [HttpPost("tao-zip/{jobId}/day-len-kho")]
        public ApiResponse DayLenKho(string jobId)
        {
            try
            {
                var result = _giayBaoService.BatDauDayLenKho(jobId);
                return new(result);
            }
            catch (Exception ex)
            {
                return OkException(ex);
            }
        }

        /// <summary>
        /// Nội dung file nén để trình duyệt tải thẳng xuống đĩa. KHÔNG bọc ApiResponse và KHÔNG đòi Bearer:
        /// trình duyệt điều hướng tới đây thì không gắn được header, nên chặn bằng token dùng riêng cho lô.
        /// </summary>
        [HttpGet("tao-zip/{jobId}/tai-ve")]
        [AllowAnonymous]
        public IActionResult TaiVe(string jobId, string token)
        {
            var job = _zipJobStore.Lay(jobId);
            if (job == null || job.TaiToken != token || !job.HoanTat || job.DuongDanZip == null)
            {
                return NotFound();
            }

            var stream = new FileStream(job.DuongDanZip, FileMode.Open, FileAccess.Read, FileShare.Read);
            return File(stream, GiayBaoConstants.ZipContentType,
                $"giay-bao-trung-tuyen-{DateTime.UtcNow:yyyyMMddHHmmss}.zip");
        }
    }
}
