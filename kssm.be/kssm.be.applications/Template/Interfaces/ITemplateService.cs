using kssm.be.applications.Template.Dtos;
using kssm.be.shared.Requests.BaseRequest;

namespace kssm.be.applications.Template.Interfaces
{
    /// <summary>
    /// Quản lý TEMPLATE CẤU HÌNH CHỮ KÝ: bộ cấu hình dựng sẵn gồm chứng thư số, lý do/nơi ký, ảnh dấu đỏ,
    /// ảnh chữ ký tươi và toạ độ từng khối, để người dùng không phải kéo thả lại từ đầu mỗi lần ký.
    ///
    /// kssm.be đứng sau sao_mai_be như một API external nên KHÔNG có người đăng nhập: template không thuộc về
    /// ai cả, chủ sở hữu do bảng template ký số bên MongoDB của sao_mai_be giữ theo Id trả về ở đây.
    /// </summary>
    public interface ITemplateService
    {
        /// <summary>
        /// Tạo template rỗng, chỉ đặt tên, và trả về Id để bên gọi lưu vào bảng của mình. Chứng thư, lý do/nơi
        /// ký, ảnh và toạ độ do màn cấu hình đặt sau qua CreateConfigAsync.
        /// </summary>
        Task<ViewTemplateDto> CreateAsync(AddTemplateDto input);

        /// <summary>Đổi tên template. Không đụng tới phần cấu hình ký.</summary>
        Task<ViewTemplateDto> UpdateAsync(UpdateTemplateDto input);

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

        /// <summary>Xoá mềm template cùng toạ độ của nó và dọn luôn ảnh trên kho object.</summary>
        Task DeleteAsync(int id);

        /// <summary>Lấy một template kèm toàn bộ toạ độ.</summary>
        Task<ViewTemplateDto> GetByIdAsync(int id);

        /// <summary>
        /// Danh sách template có phân trang, lọc theo Keyword trên tên template.
        /// Theo quy ước của PagingExtension: PageSize = -1 là lấy hết, không phân trang.
        /// </summary>
        Task<BaseResponsePagingDto<ViewTemplateDto>> FindPagingAsync(FindPagingTemplateDto input);

        /// <summary>Thông tin file PDF mẫu đi kèm bản cài, để người dùng đặt thử vị trí khi chưa có hồ sơ thật.</summary>
        SampleFileDto GetSampleFile();

        /// <summary>Nội dung file PDF mẫu để bên gọi hiển thị.</summary>
        Stream OpenSampleFile();
    }
}
