using ksts.be.external.Signing.Dtos;
using System.Security.Cryptography.X509Certificates;

namespace ksts.be.external.Signing.Interfaces
{
    /// <summary>
    /// Chỗ hẹn giữa tiến trình ký ở máy chủ và cái token cắm ở máy người dùng.
    ///
    /// Máy chủ dựng xong SignedAttributes thì bỏ vào đây rồi NGỦ chờ; phía máy người dùng lấy ra, ký bằng
    /// token, nộp chữ ký lại và đánh thức nó dậy. Qua chỗ này chỉ có SignedAttributes và chữ ký thô — khoá
    /// bí mật lẫn mã PIN không bao giờ tới máy chủ.
    ///
    /// Ai làm người đưa thư không phải việc của lớp này: hôm nay là trang web đang mở, mai có thể là plugin
    /// tự gọi ra qua WebSocket, phần lõi giữ nguyên.
    /// </summary>
    public interface IHangDoiKy
    {
        /// <summary>
        /// Mở phiên cho một lô, giữ chứng thư phần CÔNG KHAI mà máy người dùng nộp lên. Máy chủ cần nó để
        /// dựng chuỗi tin cậy và lắp vào CMS, không cần khoá riêng.
        /// </summary>
        void MoPhien(int loKyId, X509Certificate2 cert);

        /// <summary>Chứng thư của phiên. Chưa mở phiên thì ném — không có chứng thư thì không ký được gì.</summary>
        X509Certificate2 LayChungThu(int loKyId);

        /// <summary>Đóng phiên và huỷ mọi yêu cầu còn treo, để tiến trình ký không ngồi chờ vô hạn.</summary>
        void DongPhien(int loKyId);

        /// <summary>
        /// Gửi một dãy byte đi ký rồi chờ chữ ký về. Quá hạn mà không ai lấy đi ký thì ném, để file đó tính
        /// là lỗi chứ lô không treo mãi.
        /// </summary>
        Task<byte[]> XinChuKyAsync(int loKyId, byte[] duLieu, CancellationToken cancellationToken);

        /// <summary>
        /// Lấy các yêu cầu đang chờ, kiểu chờ-rồi-trả: không có việc thì giữ lời gọi tới khi có, tối đa
        /// <paramref name="cho"/>. Hỏi theo nhịp cố định sẽ cộng đúng nửa nhịp vào mỗi file, mà token ký
        /// tuần tự nên khoản đó nhân thẳng với số file.
        /// </summary>
        Task<List<YeuCauKyDto>> LayYeuCauAsync(int loKyId, TimeSpan cho, CancellationToken cancellationToken);

        /// <summary>Nộp chữ ký của cả một đợt, đánh thức các lượt ký đang chờ.</summary>
        void NopKetQua(int loKyId, IEnumerable<KetQuaKyDto> ketQua);
    }
}
