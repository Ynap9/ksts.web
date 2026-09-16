using kssm.be.applications.KySo.LoKy.Dtos;
using kssm.be.external.KySo.Pdf.Dtos;
using kssm.be.external.KySo.Signing.Dtos;
using kssm.be.infrastructure.Persistence;
using System.Threading.Channels;
using TemplateEntity = kssm.be.domain.KySo.Template.Template;

namespace kssm.be.applications.KySo.LoKy.Interfaces
{
    /// <summary>
    /// Chạy một lô ký ở NỀN: lấy file kế tiếp còn chờ, dựng bản ký, ký, đóng dấu thời gian rồi ghi kết quả.
    ///
    /// Chạy nền chứ không trong vòng đời request vì lô vài nghìn file mất hàng chục phút. Đăng ký Singleton
    /// và tự mở scope riêng cho từng file: DbContext là scoped, giữ một context suốt cả lô thì bảng theo dõi
    /// thay đổi phình theo số file.
    ///
    /// Nút thắt là MẠNG chứ không phải CPU nên nhiều file chạy song song; việc xếp hàng cho token do chính
    /// máy người dùng lo, máy chủ không khoá tuần tự phép ký.
    /// </summary>
    public interface IKySoRunner
    {
        /// <summary>Khởi động tiến trình ký cho một lô. Gọi lại trên lô đang chạy thì bỏ qua.</summary>
        void BatDau(int loKyId, string thumbprint);

        /// <summary>
        /// Dừng lô đang chạy. Chỉ ra hiệu dừng chứ KHÔNG chốt trạng thái: tạm dừng hay huỷ là quyết định của
        /// tầng nghiệp vụ, và nó đã ghi trạng thái trước khi gọi vào đây.
        /// </summary>
        void Dung(int loKyId);

        /// <summary>Lô có đang được tiến trình nền chạy không.</summary>
        bool DangChay(int loKyId);

        /// <summary>
        /// Vòng chạy của một lô: mở phiên ký một lần rồi thả nhiều luồng cùng rút việc. Lỗi của MỘT file
        /// không làm dừng lô; chỉ sự cố chung mới dừng.
        /// </summary>
        Task ChayLoAsync(int loKyId, string thumbprint, CancellationToken cancellationToken);

        /// <summary>
        /// Mở phiên ký: nạp chứng thư, chuỗi chứng thư, cấu hình template, ảnh chữ ký tươi và kho của lô
        /// ĐÚNG MỘT LẦN. Với lô vài nghìn file, làm lại theo từng file là ngần ấy vòng đi mạng thừa.
        /// </summary>
        Task<PhienKyDto> MoPhienAsync(int loKyId, string thumbprint, CancellationToken cancellationToken);

        Task ChayCoKiemSoatAsync(Func<Task> viec, CancellationTokenSource dungLo);

        Task DongKenhKhiXongAsync(IEnumerable<Task> luongKy, ChannelWriter<BanKyDto> banKy);

        Task NhanViecAsync(int loKyId, ChannelWriter<ViecKyDto> viec, CancellationToken cancellationToken);

        Task<List<ViecKyDto>> NhanDotViecAsync(int loKyId, CancellationToken cancellationToken);

        Task ChayLuongKyAsync(PhienKyDto phien, ChannelReader<ViecKyDto> viec, ChannelWriter<BanKyDto> banKy,
            CancellationToken cancellationToken);

        Task ChayLuongHoanTatAsync(PhienKyDto phien, ChannelReader<BanKyDto> banKy,
            CancellationToken cancellationToken);

        Task<BanKyDto?> DungBanKyAsync(ViecKyDto viec, PhienKyDto phien, CancellationToken cancellationToken);

        Task HoanTatBanKyAsync(BanKyDto ban, PhienKyDto phien, CancellationToken cancellationToken);

        KiemChuKyDto KiemChuKy(byte[] daKy);

        Task GhiKetQuaXongAsync(BanKyDto ban, string driveFileId, DateTime? dauThoiGian, KiemChuKyDto kiem);

        Task GhiKetQuaLoiAsync(ViecKyDto viec, string lyDo, KiemChuKyDto? kiem);

        Task TraViecDangKyAsync(int loKyId);

        void GhiNhatKyThoiGian(int thuTu, long msTong, long msTai, long msDung, long msKy, long msTsa);

        /// <summary>
        /// Tải ảnh dấu đỏ của template về, MỘT lần cho cả lô. Cùng luật với ảnh chữ ký tươi: không khai thì
        /// trả null, khai rồi mà kho không trả được ảnh thì DỪNG lô.
        /// </summary>
        Task<byte[]?> TaiAnhDauDoAsync(TemplateEntity template, CancellationToken cancellationToken);

        /// <summary>
        /// Tải ảnh chữ ký tươi của template về, MỘT lần cho cả lô. Template không khai ảnh thì trả null —
        /// chữ ký tươi là tuỳ chọn. Khai rồi mà kho không có ảnh thì DỪNG lô kèm lý do đọc được: ký thiếu
        /// ảnh cả lô rồi mới phát hiện là phải ký lại từ đầu.
        /// </summary>
        Task<byte[]?> TaiAnhChuKyTuoiAsync(TemplateEntity template, CancellationToken cancellationToken);

        /// <summary>
        /// Quy đổi cấu hình template sang tuỳ chọn dựng PDF. Hai cờ của template quyết định mặt chữ ký:
        /// <c>HienThiChuKySo</c> vẽ khối chữ ký số, <c>NhoiChuKySoVaoAnh</c> đặt widget trùm lên ảnh chữ ký
        /// tươi và con dấu. Bật cả hai vẫn chỉ MỘT chữ ký, chỉ là nhiều widget.
        /// </summary>
        PdfPrepareOptionsDto DungTuyChon(TemplateEntity template, List<PdfPlacementDto> viTri,
            string tenNguoiKy, byte[]? anhDauDo, byte[]? anhChuKyTuoi);

        /// <summary>
        /// Bản sao tuỳ chọn cho một file, chỉ khác giờ ký. Phải nhân bản chứ không sửa thẳng bản mẫu: các
        /// luồng dùng chung một bản mẫu, sửa tại chỗ là hai file ghi đè giờ ký của nhau.
        /// </summary>
        PdfPrepareOptionsDto NhanBanTuyChon(PdfPrepareOptionsDto mau, DateTime signedAt);

        /// <summary>Cộng dồn số file xong / lỗi của lô bằng một câu lệnh, không đếm lại cả bảng sau mỗi file.</summary>
        Task CongDonKetQuaAsync(KssmDbContext db, int loKyId, bool thanhCong);

        /// <summary>
        /// Chốt lô sau khi chạy hết: đánh dấu xong, dọn file nguồn của lô tải lên, và báo sang sao_mai để
        /// dự án chuyển trạng thái. Lô bị dừng giữa chừng KHÔNG đi qua đây.
        /// </summary>
        Task KetThucLoAsync(int loKyId, CancellationToken cancellationToken);

        /// <summary>Ghi sự cố chung của lô — khác lỗi từng file, cái này làm cả lô dừng lại.</summary>
        Task GhiLoiChungAsync(int loKyId, string thongDiep);
    }
}
