using ksts.be.applications.LoKy.Dtos;
using ksts.be.shared.Constants.LoKy;
using Microsoft.AspNetCore.Http;
using LoKyEntity = ksts.be.domain.LoKy.LoKy;
using LoKyFileEntity = ksts.be.domain.LoKy.LoKyFile;

namespace ksts.be.applications.LoKy.Interfaces
{
    /// <summary>Lô ký số hàng loạt: mở lô, nhận file theo đợt, bắt đầu ký, theo dõi tiến độ, tải kết quả.</summary>
    public interface ILoKyService
    {
        /// <summary>Mở một lô rỗng, trả về id để FE bắt đầu đẩy file lên.</summary>
        Task<ViewLoKyDto> TaoLoAsync(TaoLoKyDto input);

        /// <summary>
        /// Nhận MỘT đợt file tải lên từ máy người dùng. Chịu được gọi lại cùng một đợt mà không nhân đôi dòng
        /// — FE gửi lại đúng đợt hỏng, nên khử trùng theo tên file trong phạm vi lô.
        /// </summary>
        Task<ViewLoKyDto> ThemFileAsync(int loKyId, IFormFileCollection files);

        /// <summary>
        /// Nhận file từ MỘT THƯ MỤC CÓ SẴN trên kho object thay vì tải lên từ máy.
        ///
        /// File không được chép đi đâu cả: lô chỉ ghi lại object key đang có, nên bước upload biến mất hoàn
        /// toàn — với lô 5000 giấy báo vừa dựng xong đó là vài GB không phải tải về rồi đẩy lên lần nữa.
        /// </summary>
        Task<ViewLoKyDto> ThemFileTuKhoAsync(int loKyId, ThemFileTuKhoDto input);

        /// <summary>
        /// Quy đường dẫn người dùng dán vào thành TIỀN TỐ object key. Nhận cả link đầy đủ của kho, đường dẫn
        /// kèm tên bucket, hay chỉ mỗi tên thư mục — dán từ trình duyệt kho ra kiểu nào cũng phải chạy.
        /// </summary>
        string ChuanHoaTienTo(string duongDan);

        /// <summary>
        /// Nhận chứng thư phần CÔNG KHAI do máy người dùng nộp và mở phiên ký cho lô. Gọi TRƯỚC khi bắt đầu:
        /// không có chứng thư thì tiến trình ký không dựng nổi SignedAttributes.
        ///
        /// Máy chủ tự thẩm định chuỗi tin cậy của chứng thư này, không tin cờ nào do máy người dùng gửi lên.
        /// </summary>
        Task<ViewLoKyDto> MoPhienKyAsync(int loKyId, MoPhienKyDto input);

        /// <summary>Đóng phiên ký và huỷ mọi lượt còn treo, gọi khi lô xong hoặc người dùng rời màn hình.</summary>
        Task DongPhienKyAsync(int loKyId);

        /// <summary>Kiểm template và chứng thư rồi đẩy lô vào tiến trình ký nền.</summary>
        Task<ViewLoKyDto> BatDauAsync(int loKyId, BatDauKyDto input);

        /// <summary>
        /// Danh sách file của lô, lấy MỘT lần khi mở màn hình. Tách khỏi tiến độ vì tiến độ được hỏi mỗi vài
        /// giây: kèm cả nghìn dòng vào mỗi nhịp là thứ làm trình duyệt cạn tài nguyên rồi chết giữa lô.
        /// </summary>
        Task<List<ViewFileKyDto>> DanhSachFileAsync(int loKyId);

        /// <summary>Tiến độ và danh sách file LỖI để FE hỏi theo nhịp.</summary>
        Task<ViewTienDoDto> TrangThaiAsync(int loKyId);

        /// <summary>Lô còn dở của người đang đăng nhập, để mở lại màn hình là thấy đúng tiến độ.</summary>
        Task<ViewLoKyDto?> LoDangChayAsync();

        /// <summary>
        /// Pauses the batch: files left mid-signing go back to the queue and the source files stay, so the
        /// next start picks up from the next file.
        /// </summary>
        Task DungAsync(int loKyId);

        /// <summary>
        /// Cancels the batch for good. Signed files stay on the store and stay valid, but the batch never
        /// resumes and its uploaded source files are cleaned up.
        /// </summary>
        Task HuyAsync(int loKyId);

        /// <summary>
        /// Kéo bản đã ký từ kho về rồi nén thẳng vào <paramref name="dich"/> - là luồng gửi cho trình duyệt.
        /// Không có file nén trung gian trên đĩa máy chủ: lô vài nghìn giấy báo lên tới hàng GB, đủ làm đầy ổ.
        ///
        /// Chặn bằng <paramref name="taiToken"/> chứ không bằng Bearer: trình duyệt điều hướng thẳng tới đường
        /// dẫn này để tải file nên KHÔNG gắn được header Authorization.
        /// </summary>
        Task GhiNenAsync(int loKyId, string taiToken, Stream dich, CancellationToken cancellationToken);

        /// <summary>Kéo một bản đã ký từ kho về. Hỏng thì trả null để khâu nén bỏ qua file đó.</summary>
        Task<byte[]?> TaiMotFileAsync(string objectKey, CancellationToken cancellationToken);

        /// <summary>Lấy lô kèm kiểm quyền sở hữu — người dùng khác không được đụng vào lô không phải của mình.</summary>
        Task<LoKyEntity> LayLoAsync(int loKyId);

        /// <summary>Quy đổi một dòng file sang DTO hiển thị.</summary>
        ViewFileKyDto ToViewFileDto(LoKyFileEntity file);

        /// <summary>Mã trạng thái file gửi cho FE dưới dạng chuỗi thay vì số thứ tự enum.</summary>
        string MaTrangThai(TrangThaiFileKy trangThai);

        /// <summary>Quy đổi entity sang DTO hiển thị.</summary>
        ViewLoKyDto ToViewDto(LoKyEntity lo);
    }
}
