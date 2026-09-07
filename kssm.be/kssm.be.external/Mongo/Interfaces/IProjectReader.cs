using kssm.be.external.Mongo.Dtos;
using kssm.be.external.S3.Dtos;

namespace kssm.be.external.Mongo.Interfaces
{
    /// <summary>
    /// Đọc dữ liệu dự án từ MongoDB của sao_mai. CHỈ ĐỌC: mọi thay đổi nghiệp vụ bên đó vẫn do sao_mai_be
    /// làm, service này chỉ cần biết dự án nào ký được, file của nó nằm ở đâu và kho nào giữ chúng.
    /// </summary>
    public interface IProjectReader
    {
        /// <summary>Dự án đang nghiệm thu — bộ dự án duy nhất được phép đưa vào ký số.</summary>
        Task<IReadOnlyList<ProjectDto>> GetSignableProjectsAsync(CancellationToken cancellationToken = default);

        /// <summary>Một dự án theo Id. Không thấy hoặc đã xoá mềm thì ném lỗi không tìm thấy dự án.</summary>
        Task<ProjectDto> GetProjectAsync(string projectId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Kho MinIO riêng của dự án, hoặc null khi dự án khai MinIO mà chưa nhập đủ khoá — lúc đó sao_mai_be
        /// đã lùi về MinIO của biến môi trường khi nhận file, nên bên gọi dùng kho mặc định. Dự án lưu file ở
        /// nơi khác MinIO thì ném lỗi NGAY: phát hiện lúc mở lô rẻ hơn hẳn khi đã ký dở vài trăm file.
        /// </summary>
        Task<S3ConnectionDto?> GetProjectStorageAsync(string projectId, CancellationToken cancellationToken = default);

        /// <summary>
        /// File PDF của dự án, sắp theo thứ tự hiển thị bên sao_mai. Đường dẫn lưu trong Mongo là URL đầy đủ
        /// nên phải cắt về object key theo đúng kho của dự án; URL không thuộc kho đó bị bỏ qua.
        /// </summary>
        Task<IReadOnlyList<ProjectFileDto>> GetPdfFilesAsync(string projectId, S3ConnectionDto storage,
            CancellationToken cancellationToken = default);
    }
}
