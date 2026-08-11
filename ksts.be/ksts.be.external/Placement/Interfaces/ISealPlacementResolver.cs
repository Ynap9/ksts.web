using ksts.be.external.Images.Dtos;
using ksts.be.external.Pdf.Dtos;
using ksts.be.external.Placement.Dtos;

namespace ksts.be.external.Placement.Interfaces
{
    /// <summary>
    /// Chốt VỊ TRÍ đặt con dấu và chữ ký tươi trên một trang PDF.
    ///
    /// Điểm đặt là TRUNG ĐIỂM đoạn nối dòng chức danh người ký với dòng tên người ký - đúng chỗ trống mà con
    /// dấu được đóng trên bản giấy thật. Suy từ chữ trên trang chứ không từ khổ giấy, nên đổi người ký hay
    /// đổi độ dài tên đều không làm lệch.
    /// </summary>
    public interface ISealPlacementResolver
    {
        /// <summary>
        /// Tính ô đặt dấu / chữ ký tươi trên trang <paramref name="pageNumber"/>.
        /// Hai tham số ảnh đều TUỲ CHỌN: null nghĩa là template không dùng khối đó, ô tương ứng trả null.
        /// Không tìm được mốc thì ném UserFriendlyException(SealAnchorNotFound) chứ KHÔNG tự đoán một vị trí
        /// mặc định - đóng dấu sai chỗ trên giấy báo trúng tuyển tệ hơn hẳn báo lỗi để người dùng đặt tay.
        /// </summary>
        SealPlacementResultDto Resolve(Stream pdfStream, int pageNumber, ImageSizeDto? dauDo,
            ImageSizeDto? chuKyTuoi);

        /// <summary>
        /// Tìm dòng CHỨC DANH người ký. Duyệt hằng số theo thứ tự cụ thể trước tổng quát nên dòng
        /// "PHÓ HIỆU TRƯỞNG" không bị khớp nhầm vào mục "HIỆU TRƯỞNG".
        /// </summary>
        PdfTextLineDto? FindAnchorChucDanh(IReadOnlyList<PdfTextLineDto> lines);

        /// <summary>
        /// Tìm dòng TÊN NGƯỜI KÝ nằm dưới mốc chức danh. Ưu tiên khớp tên khai trong hằng số; không khớp thì
        /// lùi về dòng gần nhất bên dưới có tâm ngang thẳng cột với mốc trên và mở đầu bằng học hàm/học vị.
        /// Quy tắc lùi giữ cho tính năng chạy khi đổi người ký mà chưa kịp sửa hằng số.
        /// </summary>
        PdfTextLineDto? FindAnchorTenNguoiKy(IReadOnlyList<PdfTextLineDto> lines, PdfTextLineDto anchorChucDanh);

        /// <summary>
        /// Dựng ô có tâm tại (<paramref name="centerXRatio"/>, <paramref name="centerYRatio"/>) với kích thước
        /// cho trước tính bằng point. Ô tràn mép trang thì DỊCH vào trong, tuyệt đối không thu nhỏ: dịch chỗ
        /// thì con dấu vẫn đúng cỡ, thu nhỏ là làm sai con dấu.
        /// </summary>
        PdfRectRatioDto BuildCenteredRect(double centerXRatio, double centerYRatio, double widthPoints,
            double heightPoints, double pageWidthPoints, double pageHeightPoints);
    }
}
