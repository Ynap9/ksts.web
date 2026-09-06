using kssm.be.applications.LoKy.Dtos;
using kssm.be.external.Signing.Dtos;
using Microsoft.AspNetCore.Http;

namespace kssm.be.applications.LoKy.Interfaces
{
    /// <summary>
    /// Lô ký số hàng loạt. Hai nguồn file: file người dùng tải lên (ghi vào kho mặc định của service) và
    /// file của một dự án bên sao_mai (đọc và ghi ngay trên MinIO của chính dự án đó).
    ///
    /// Không có auth: bên gọi đã xác thực người dùng từ trước, nên id người tạo chỉ để lọc và truy vết.
    /// </summary>
    public interface ILoKyService
    {
        /// <summary>Dự án được phép ký — chỉ dự án đang nghiệm thu, dùng cho ô chọn dự án ở màn ký số.</summary>
        Task<List<ViewProjectDto>> DanhSachDuAnAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Mở lô. Có <c>projectId</c> thì lô nạp luôn toàn bộ file PDF của dự án và chốt kho theo dự án;
        /// không có thì lô chờ file tải lên và dùng kho mặc định.
        /// </summary>
        Task<ViewLoKyDto> TaoLoAsync(TaoLoKyDto input, CancellationToken cancellationToken = default);

        /// <summary>
        /// Nhận một đợt file tải lên. Chỉ dùng cho lô KHÔNG gắn dự án; file của dự án đã nằm sẵn trên kho
        /// nên không có lý do gì để tải lên lần nữa.
        /// </summary>
        Task<ViewLoKyDto> ThemFileAsync(int loKyId, IFormFileCollection files,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Nhận chứng thư phần công khai của phiên ký ở máy người dùng, tự dựng chuỗi tin cậy rồi mở phiên.
        /// Phải gọi TRƯỚC khi bắt đầu ký: thiếu bước này thì lô chạy nhưng không ai ký được.
        /// </summary>
        Task<ViewLoKyDto> MoPhienKyAsync(int loKyId, MoPhienKyDto input);

        /// <summary>Đóng phiên ký và huỷ mọi lượt còn treo.</summary>
        Task DongPhienKyAsync(int loKyId);

        /// <summary>
        /// Bắt đầu — hoặc ký tiếp sau khi tạm dừng. Luôn chạy từ file còn ở trạng thái chờ nên gọi lại là
        /// tiếp tục chứ không ký đè lên file đã xong.
        /// </summary>
        Task<ViewLoKyDto> BatDauAsync(int loKyId, BatDauKyDto input);

        /// <summary>Lấy các yêu cầu ký đang chờ; chưa có việc thì giữ lời gọi lại tới khi có hoặc hết hạn chờ.</summary>
        Task<List<YeuCauKyDto>> LayYeuCauKyAsync(int loKyId, CancellationToken cancellationToken);

        /// <summary>
        /// Nộp chữ ký của cả một đợt. Trong đợt có kết quả báo phiên ký đã mất thì lô chuyển sang tạm dừng
        /// ngay, thay vì để từng file lần lượt hết hạn rồi bị tính là lỗi.
        /// </summary>
        Task NopChuKyAsync(int loKyId, List<NopChuKyDto> input);

        /// <summary>Tạm dừng lô: file đang ký trả về hàng đợi, bấm ký tiếp là chạy lại từ đúng chỗ đó.</summary>
        Task<ViewLoKyDto> TamDungAsync(int loKyId, TamDungKyDto input);

        /// <summary>Đóng lô hẳn. File đã ký vẫn giữ nguyên và vẫn hợp lệ; muốn ký nữa phải mở lô mới.</summary>
        Task<ViewLoKyDto> HuyAsync(int loKyId);

        /// <summary>Danh sách đầy đủ các file của lô, lấy MỘT lần lúc mở màn hình.</summary>
        Task<List<ViewFileKyDto>> DanhSachFileAsync(int loKyId);

        /// <summary>
        /// Tiến độ để hỏi theo nhịp: bộ đếm, file lỗi, một ít file vừa xong, và cờ phiên ký còn sống hay
        /// không. Không bao giờ trả cả danh sách file.
        /// </summary>
        Task<ViewTienDoDto> TrangThaiAsync(int loKyId);

        /// <summary>Lô đang chạy dở của một người, để mở lại màn hình thấy đúng lô cũ.</summary>
        Task<ViewLoKyDto?> LoDangChayAsync(string? nguoiTaoId);

        /// <summary>
        /// Nén các bản đã ký và ghi thẳng vào luồng gửi cho trình duyệt. Không dựng file nén trên đĩa: lô
        /// vài nghìn file là vài GB.
        /// </summary>
        Task GhiNenAsync(int loKyId, string taiToken, Stream dich, CancellationToken cancellationToken);
    }
}
