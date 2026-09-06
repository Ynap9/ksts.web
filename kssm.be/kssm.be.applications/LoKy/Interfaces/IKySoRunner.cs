using kssm.be.applications.LoKy.Dtos;
using kssm.be.external.Pdf.Dtos;
using TemplateEntity = kssm.be.domain.Template.Template;

namespace kssm.be.applications.LoKy.Interfaces
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

        /// <summary>Một luồng thợ: rút việc kế tiếp rồi ký, tới khi hết việc hoặc lô bị dừng.</summary>
        Task ChayMotLuongAsync(PhienKyDto phien, CancellationToken cancellationToken);

        /// <summary>
        /// Nhận file kế tiếp còn ở trạng thái chờ và đánh dấu đang ký, trả null khi hết việc. Việc nhận được
        /// khoá lại để hai luồng không bao giờ nhận trúng cùng một file.
        /// </summary>
        Task<int?> NhanViecAsync(int loKyId, CancellationToken cancellationToken);

        /// <summary>
        /// Ký đúng một file rồi đẩy luôn bản ký lên kho của lô.
        ///
        /// Fail-closed với dấu thời gian: TSA hỏng sau các lần thử thì file bị đánh lỗi chứ KHÔNG bao giờ
        /// phát hành bản ký thiếu dấu thời gian. Lô bị dừng giữa chừng thì file đang ký được trả về hàng đợi
        /// để lần chạy sau ký lại từ đúng chỗ này, không tính là lỗi.
        /// </summary>
        Task KyMotFileAsync(int loKyFileId, PhienKyDto phien, CancellationToken cancellationToken);

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
            string tenNguoiKy, byte[]? anhChuKyTuoi);

        /// <summary>
        /// Bản sao tuỳ chọn cho một file, chỉ khác giờ ký. Phải nhân bản chứ không sửa thẳng bản mẫu: các
        /// luồng dùng chung một bản mẫu, sửa tại chỗ là hai file ghi đè giờ ký của nhau.
        /// </summary>
        PdfPrepareOptionsDto NhanBanTuyChon(PdfPrepareOptionsDto mau, DateTime signedAt);

        /// <summary>Cộng dồn số file xong / lỗi của lô bằng một câu lệnh, không đếm lại cả bảng sau mỗi file.</summary>
        Task CongDonKetQuaAsync(int loKyId, bool thanhCong);

        /// <summary>
        /// Chốt lô sau khi chạy hết: đánh dấu xong, dọn file nguồn của lô tải lên, và báo sang sao_mai để
        /// dự án chuyển trạng thái. Lô bị dừng giữa chừng KHÔNG đi qua đây.
        /// </summary>
        Task KetThucLoAsync(int loKyId, CancellationToken cancellationToken);

        /// <summary>Ghi sự cố chung của lô — khác lỗi từng file, cái này làm cả lô dừng lại.</summary>
        Task GhiLoiChungAsync(int loKyId, string thongDiep);
    }
}
