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

        /// <summary>Số mm trong một inch.</summary>
        public const double MmPerInch = 25.4;

        /// <summary>
        /// Bề rộng con dấu khi in, tính bằng mm thật — lấy đúng bản dựng bên ksts (giay-bao-trung-tuyen.html,
        /// rule .signature-stamp). Dấu KHÔNG co theo ô kéo trên template và KHÔNG theo DPI của ảnh: ảnh dấu
        /// thường không khai DPI, tin vào đó thì cùng một con dấu ra mỗi máy một cỡ.
        /// </summary>
        public const double DauDoWidthMm = 36;

        /// <summary>
        /// Trần bề rộng CHỮ KÝ TƯƠI theo tỉ lệ bề rộng trang. Chữ ký tươi được co giãn (khác con dấu), nhưng
        /// ảnh quét có thể rất lớn nên phải có trần, nếu không chữ ký chiếm hết nửa trang.
        /// </summary>
        public const double MaxChuKyTuoiWidthRatio = 0.25;
    }
}
