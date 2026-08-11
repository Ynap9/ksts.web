using ksts.be.external.Certificates.Dtos;

namespace ksts.be.external.Certificates.Interfaces
{
    /// <summary>
    /// Nguồn chứng thư số dùng để ký.
    ///
    /// Implement hiện tại đọc Windows certificate store của MÁY ĐANG CHẠY API. Với KSTS là web, đó chỉ đúng ở
    /// môi trường dev (BE và trình duyệt cùng máy) - trên server thật nó đọc cert của server chứ không phải
    /// của người dùng. Interface tách riêng chính là để đổi nguồn sang agent chạy ở máy client mà không phải
    /// sửa service lẫn controller. Xem .claude/docs/ky-so-web-vs-desktop.md.
    /// </summary>
    public interface ICertificateProvider
    {
        /// <summary>
        /// Liệt kê MỌI chứng thư đọc được, gồm cả cert KHÔNG ký được - kèm cờ CanSign và
        /// <see cref="SignCertDto.Reason"/> để người dùng biết vì sao một cert không chọn được; việc lọc do
        /// tầng service quyết định. Danh sách rỗng mà không rõ nguyên nhân là kiểu lỗi tốn cả buổi để chẩn đoán.
        ///
        /// Chỉ đọc METADATA của khoá, KHÔNG dùng khoá để ký nên KHÔNG bật hộp thoại nhập PIN.
        /// Store nào mở lỗi thì bỏ qua store đó chứ không ném - LocalMachine thường không mở được khi chạy
        /// quyền user thường, đó là chuyện bình thường. Lý do bỏ qua ghi vào
        /// <see cref="CertScanResultDto.StoreDiagnostics"/> để chẩn đoán được trên máy không có debugger.
        /// </summary>
        CertScanResultDto GetCertificates();
    }
}
