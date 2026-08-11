using ksts.be.applications.LoKy.Dtos;
using ksts.be.shared.Constants.LoKy;
using Microsoft.AspNetCore.Http;
using LoKyEntity = ksts.be.domain.LoKy.LoKy;

namespace ksts.be.applications.LoKy.Interfaces
{
    /// <summary>Lô ký số hàng loạt: mở lô, nhận file theo đợt, bắt đầu ký, theo dõi tiến độ, tải kết quả.</summary>
    public interface ILoKyService
    {
        /// <summary>Mở một lô rỗng, trả về id để FE bắt đầu đẩy file lên.</summary>
        Task<ViewLoKyDto> TaoLoAsync(TaoLoKyDto input);

        /// <summary>
        /// Nhận MỘT đợt file. Chịu được gọi lại cùng một đợt mà không nhân đôi dòng — FE gửi lại đúng đợt hỏng,
        /// nên khử trùng theo tên file trong phạm vi lô.
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

        /// <summary>Kiểm template và chứng thư rồi đẩy lô vào tiến trình ký nền.</summary>
        Task<ViewLoKyDto> BatDauAsync(int loKyId, BatDauKyDto input);

        /// <summary>
        /// Mở việc đẩy bản đã ký lên thư mục dùng chung của kho, chạy nền. Độc lập với việc tải zip về: làm
        /// một trong hai hay cả hai đều được sau khi lô ký xong.
        /// </summary>
        Task<ViewLoKyDto> BatDauDayLenKhoAsync(int loKyId);

        /// <summary>Tiến độ và danh sách file để FE hỏi theo nhịp.</summary>
        Task<ViewTienDoDto> TrangThaiAsync(int loKyId);

        /// <summary>Lô còn dở của người đang đăng nhập, để mở lại màn hình là thấy đúng tiến độ.</summary>
        Task<ViewLoKyDto?> LoDangChayAsync();

        /// <summary>Dừng lô. File đã ký xong giữ nguyên và vẫn hợp lệ.</summary>
        Task HuyAsync(int loKyId);

        /// <summary>
        /// Gói các file đã ký thành zip. Ghi ra file tạm rồi trả stream chứ không gom vào RAM: lô vài nghìn
        /// giấy báo lên tới hàng GB.
        ///
        /// Chặn bằng <paramref name="taiToken"/> chứ không bằng Bearer: trình duyệt điều hướng thẳng tới đường
        /// dẫn này để tải file nên KHÔNG gắn được header Authorization.
        /// </summary>
        Task<Stream> TaiZipAsync(int loKyId, string taiToken);

        /// <summary>Lấy lô kèm kiểm quyền sở hữu — người dùng khác không được đụng vào lô không phải của mình.</summary>
        Task<LoKyEntity> LayLoAsync(int loKyId);

        /// <summary>Mã trạng thái file gửi cho FE dưới dạng chuỗi thay vì số thứ tự enum.</summary>
        string MaTrangThai(TrangThaiFileKy trangThai);

        /// <summary>Quy đổi entity sang DTO hiển thị.</summary>
        ViewLoKyDto ToViewDto(LoKyEntity lo);
    }
}
