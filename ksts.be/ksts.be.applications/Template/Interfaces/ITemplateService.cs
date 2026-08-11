using ksts.be.applications.Template.Dtos;
using ksts.be.shared.HttpRequest.BaseRequest;

namespace ksts.be.applications.Template.Interfaces
{
    /// <summary>
    /// Quản lý TEMPLATE CẤU HÌNH CHỮ KÝ: bộ cấu hình dựng sẵn gồm chứng thư số, lý do/nơi ký, ảnh dấu đỏ,
    /// ảnh chữ ký tươi và toạ độ từng khối, để người dùng không phải kéo thả lại từ đầu mỗi lần ký.
    ///
    /// Template thuộc về NGƯỜI DÙNG tạo ra nó; tài khoản admin xem được của mọi người.
    /// </summary>
    public interface ITemplateService
    {
        /// <summary>
        /// Tạo template rỗng, chỉ đặt tên. Chứng thư, lý do/nơi ký, ảnh và toạ độ do màn cấu hình đặt sau
        /// qua CreateConfigAsync.
        /// </summary>
        ViewTemplateDto Create(AddTemplateDto input);

        /// <summary>Đổi tên template. Không đụng tới phần cấu hình ký.</summary>
        ViewTemplateDto Update(UpdateTemplateDto input);

        /// <summary>
        /// Đặt cấu hình ký lần đầu cho một template đã có: chứng thư, lý do/nơi ký, ảnh dấu đỏ, ảnh chữ ký
        /// tươi và toạ độ từng khối.
        /// </summary>
        Task<ViewTemplateDto> CreateConfigAsync(AddConfigTemplateDto input);

        /// <summary>
        /// Ghi đè toàn bộ cấu hình ký, kể cả danh sách toạ độ. Không gửi ảnh nghĩa là GIỮ NGUYÊN ảnh cũ;
        /// muốn bỏ ảnh phải bật cờ XoaAnhDauDo / XoaAnhChuKyTuoi - multipart không phân biệt được
        /// "không gửi trường" với "gửi null" như JSON.
        /// </summary>
        Task<ViewTemplateDto> UpdateConfigAsync(UpdateConfigTemplateDto input);

        /// <summary>Xoá mềm template và dọn luôn ảnh của nó trên MinIO.</summary>
        Task DeleteAsync(int id);

        /// <summary>Lấy một template kèm toàn bộ toạ độ.</summary>
        Task<ViewTemplateDto> GetByIdAsync(int id);

        /// <summary>
        /// Danh sách template có phân trang, lọc theo Keyword trên tên template và tên chứng thư.
        /// Theo quy ước của PagingExtension: PageSize = -1 là lấy hết, không phân trang.
        /// </summary>
        Task<BaseResponsePagingDto<ViewTemplateDto>> FindPagingAsync(FindPagingTemplateDto input);

        /// <summary>Thông tin file PDF mẫu đi kèm app để người dùng đặt thử vị trí khi chưa có hồ sơ thật.</summary>
        SampleFileDto GetSampleFile();

        /// <summary>Nội dung file PDF mẫu để FE hiển thị.</summary>
        Stream OpenSampleFile();

        /// <summary>
        /// Vị trí GỢI Ý cho con dấu và chữ ký tươi: trung điểm đoạn nối chức danh người ký với tên người ký,
        /// dò trực tiếp từ chữ trên trang. Không gửi file PDF thì dùng file mẫu đi kèm app.
        /// Con dấu giữ nguyên kích thước gốc của ảnh, chữ ký tươi được co giãn.
        /// </summary>
        Task<SuggestedPlacementDto> GetSuggestedPlacementAsync(SuggestPlacementDto input);
    }
}
