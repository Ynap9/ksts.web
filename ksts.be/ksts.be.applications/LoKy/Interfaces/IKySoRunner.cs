using ksts.be.applications.LoKy.Dtos;
using ksts.be.external.Pdf.Dtos;
using TemplateEntity = ksts.be.domain.Template.Template;

namespace ksts.be.applications.LoKy.Interfaces
{
    /// <summary>
    /// Chạy một lô ký ở NỀN: lấy file kế tiếp còn chờ, dựng bản ký, ký, đóng dấu thời gian rồi ghi kết quả.
    ///
    /// Chạy nền chứ không trong vòng đời request vì lô vài nghìn file mất hàng chục phút — người dùng đóng tab
    /// thì lô vẫn phải ký tiếp. Đăng ký Singleton và tự mở scope riêng cho từng file: DbContext là scoped, mà
    /// giữ một context suốt cả lô thì bảng theo dõi thay đổi phình theo số file.
    ///
    /// Nút thắt là MẠNG chứ không phải CPU (đo thực: mật mã 6 ms/file, TSA 15 ms, còn lại là MinIO), nên nhiều
    /// file chạy song song. Riêng phép KÝ vẫn tuần tự — xem <see cref="KyMotFileAsync"/>.
    /// </summary>
    public interface IKySoRunner
    {
        /// <summary>Khởi động tiến trình ký cho một lô. Gọi lại trên lô đang chạy thì bỏ qua.</summary>
        void BatDau(int loKyId, string thumbprint);

        /// <summary>Dừng lô. File đã ký xong giữ nguyên và vẫn hợp lệ, cắm lại token là chạy tiếp từ file dở.</summary>
        void Dung(int loKyId);

        /// <summary>Lô có đang được tiến trình nền chạy không.</summary>
        bool DangChay(int loKyId);

        /// <summary>
        /// Vòng chạy của một lô: mở phiên ký một lần rồi thả nhiều luồng cùng rút việc từ hàng đợi.
        /// Lỗi của MỘT file không làm dừng lô; chỉ sự cố chung (mất khoá ký, huỷ) mới dừng.
        /// </summary>
        Task ChayLoAsync(int loKyId, string thumbprint, CancellationToken cancellationToken);

        /// <summary>
        /// Mở phiên ký: nạp chứng thư, chuỗi chứng thư, cấu hình template và ảnh chữ ký tươi ĐÚNG MỘT LẦN cho
        /// cả lô. Trước đây mỗi file đều truy vấn template và tải lại ảnh từ kho — với lô vài nghìn file thì
        /// đó là ngần ấy vòng đi mạng thừa.
        /// </summary>
        Task<PhienKyDto> MoPhienAsync(int loKyId, string thumbprint, CancellationToken cancellationToken);

        /// <summary>Một luồng thợ: rút việc kế tiếp rồi ký, tới khi hết việc hoặc lô bị dừng.</summary>
        Task ChayMotLuongAsync(PhienKyDto phien, CancellationToken cancellationToken);

        /// <summary>
        /// Nhận file kế tiếp còn ở trạng thái Cho và đánh dấu đang ký, trả null khi hết việc. Việc nhận được
        /// khoá lại để hai luồng không bao giờ nhận trúng cùng một file.
        /// </summary>
        Task<int?> NhanViecAsync(int loKyId, CancellationToken cancellationToken);

        /// <summary>
        /// Ký đúng một file. Fail-closed với dấu thời gian: TSA hỏng sau các lần thử thì file bị đánh lỗi chứ
        /// KHÔNG bao giờ phát hành bản ký thiếu dấu thời gian.
        ///
        /// Phép KÝ được khoá cho chạy tuần tự dù các file chạy song song: token phần cứng chỉ có MỘT phiên và
        /// ký lần lượt, nên giữ đúng hình dạng đó ngay từ bây giờ để lúc lắp plugin vào không phải sửa lại.
        /// </summary>
        Task KyMotFileAsync(int loKyFileId, PhienKyDto phien, CancellationToken cancellationToken);

        /// <summary>
        /// Tải ảnh chữ ký tươi của template về, MỘT lần cho cả lô. Template không khai ảnh thì trả null —
        /// chữ ký tươi là tuỳ chọn. Khai rồi mà kho không có ảnh thì DỪNG lô kèm lý do đọc được, không ký
        /// tiếp: ký thiếu ảnh cả lô rồi mới phát hiện là phải ký lại từ đầu.
        /// </summary>
        Task<byte[]?> TaiAnhChuKyTuoiAsync(TemplateEntity template, CancellationToken cancellationToken);

        /// <summary>
        /// Quy đổi cấu hình template sang tuỳ chọn dựng PDF. Hai cờ của template quyết định mặt chữ ký:
        /// <c>HienThiChuKySo</c> vẽ khối chữ ký số, <c>NhoiChuKySoVaoAnh</c> đặt widget trùm lên ảnh chữ ký
        /// tươi và con dấu. Bật cả hai vẫn chỉ MỘT chữ ký, chỉ là nhiều widget.
        /// </summary>
        PdfPrepareOptionsDto DungTuyChon(TemplateEntity template, string tenNguoiKy, byte[]? anhChuKyTuoi);

        /// <summary>
        /// Bản sao tuỳ chọn cho một file, chỉ khác giờ ký. Phải nhân bản chứ không sửa thẳng bản mẫu: các
        /// luồng dùng chung một bản mẫu, sửa tại chỗ là hai file ghi đè giờ ký của nhau.
        /// </summary>
        PdfPrepareOptionsDto NhanBanTuyChon(PdfPrepareOptionsDto mau, DateTime signedAt);

        /// <summary>Cộng dồn số file xong / lỗi của lô bằng một câu lệnh, không đếm lại cả bảng sau mỗi file.</summary>
        Task CongDonKetQuaAsync(int loKyId, bool thanhCong);

        /// <summary>Chốt trạng thái lô khi vòng chạy kết thúc, dù là chạy hết hay bị dừng giữa chừng.</summary>
        Task KetThucLoAsync(int loKyId, bool biHuy);

        /// <summary>Ghi sự cố chung của lô — khác lỗi từng file, cái này làm cả lô dừng lại.</summary>
        Task GhiLoiChungAsync(int loKyId, string thongDiep);
    }
}
