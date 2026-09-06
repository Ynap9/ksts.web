namespace kssm.be.shared.Constants.Signing
{
    /// <summary>
    /// Số đo khi đặt ảnh dấu đỏ và chữ ký tươi lên trang. Ảnh đặt đúng chỗ người dùng kéo thả trong template
    /// nên ở đây không có mốc dò chữ nào, chỉ còn phép quy đổi pixel sang point và trần bề rộng.
    /// </summary>
    public static class ImagePlacementConstants
    {
        /// <summary>
        /// DPI dùng khi ảnh KHÔNG khai metadata độ phân giải. 96 là mặc định của Windows, cũng là DPI mà phần
        /// lớn công cụ chụp/cắt ảnh trên Windows ghi ra.
        /// </summary>
        public const double DefaultImageDpi = 96;

        /// <summary>Số point trong một inch — hằng số của hệ toạ độ PDF, dùng để quy pixel sang point.</summary>
        public const double PointsPerInch = 72;

        /// <summary>
        /// Trần bề rộng CHỮ KÝ TƯƠI theo tỉ lệ bề rộng trang. Chữ ký tươi được co giãn (khác con dấu), nhưng
        /// ảnh quét có thể rất lớn nên phải có trần, nếu không chữ ký chiếm hết nửa trang.
        /// CON DẤU KHÔNG áp trần này — dấu luôn giữ nguyên kích thước gốc.
        /// </summary>
        public const double MaxChuKyTuoiWidthRatio = 0.25;
    }
}
