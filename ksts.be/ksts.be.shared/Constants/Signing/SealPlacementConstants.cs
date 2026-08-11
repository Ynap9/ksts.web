namespace ksts.be.shared.Constants.Signing
{
    /// <summary>
    /// Hằng số cho việc ĐẶT CON DẤU và CHỮ KÝ TƯƠI lên trang.
    /// Khác <see cref="SigningConstants"/> (khối chữ ký số - 2 dòng chữ): file này lo hai khối ẢNH, và điểm
    /// đặt của chúng suy ra từ CHỮ TRÊN TRANG chứ không phải từ khổ giấy.
    /// </summary>
    public static class SealPlacementConstants
    {
        // ===== Mốc chữ =====
        // Dấu và chữ ký tươi đặt tại TRUNG ĐIỂM đoạn nối chức danh người ký với tên người ký - đúng chỗ trống
        // mà con dấu được đóng trên bản giấy thật. Dò theo chữ chứ không chốt cứng toạ độ: giấy báo sinh hàng
        // loạt từ một mẫu nhưng tên và chức danh người ký đổi được (đổi phó hiệu trưởng phụ trách), chốt cứng
        // là lệch ngay lần đổi đầu tiên.

        /// <summary>
        /// Chuỗi nhận diện dòng CHỨC DANH (mốc trên). Xếp từ cụ thể tới tổng quát: "PHÓ HIỆU TRƯỞNG" phải
        /// đứng trước "HIỆU TRƯỞNG", nếu không dòng phó sẽ khớp nhầm vào mục hiệu trưởng.
        /// </summary>
        public static readonly string[] AnchorChucDanh =
        {
            "PHO HIEU TRUONG",
            "QUYEN HIEU TRUONG",
            "HIEU TRUONG",
        };

        /// <summary>
        /// Chuỗi nhận diện dòng TÊN NGƯỜI KÝ (mốc dưới). Không khớp được thì lùi về quy tắc suy ra ở
        /// <see cref="AnchorAlignTolerance"/> - giữ cho tính năng chạy khi đổi người ký mà chưa kịp sửa hằng số.
        /// </summary>
        public static readonly string[] AnchorTenNguoiKy =
        {
            "PGS.TS BUI PHU DOANH",
            "PGS.TS.BUI PHU DOANH",
        };

        /// <summary>
        /// Tiền tố học hàm/học vị để nhận ra một dòng là TÊN NGƯỜI KÝ khi không khớp
        /// <see cref="AnchorTenNguoiKy"/>. Dòng tên người ký trên văn bản hành chính hầu như luôn mở đầu bằng
        /// một trong các tiền tố này.
        /// </summary>
        public static readonly string[] AnchorHocViPrefixes =
        {
            "GS.TSKH", "PGS.TS", "GS.TS", "TSKH", "PGS", "TS.", "THS.", "THS", "CN.",
        };

        /// <summary>
        /// Dung sai canh cột khi suy mốc dưới: tâm ngang của dòng ứng viên lệch tâm mốc trên không quá ngần
        /// này lần bề rộng trang thì coi là cùng một khối ký. Khối ký nằm lệch hẳn về một bên trang nên 0.15
        /// đủ rộng để bắt dòng tên dài/ngắn khác nhau mà vẫn không chạm sang cột bên kia.
        /// </summary>
        public const double AnchorAlignTolerance = 0.15;

        /// <summary>Số dòng tối đa dò xuống dưới mốc trên để tìm mốc tên - quá xa thì không còn là một khối ký.</summary>
        public const int AnchorMaxLinesBelow = 6;

        // ===== Kích thước =====
        /// <summary>
        /// DPI dùng khi ảnh KHÔNG khai metadata độ phân giải. 96 là mặc định của Windows, cũng là DPI mà phần
        /// lớn công cụ chụp/cắt ảnh trên Windows ghi ra.
        /// </summary>
        public const double DefaultImageDpi = 96;

        /// <summary>Số point trong một inch - hằng số của hệ toạ độ PDF, dùng để quy pixel sang point.</summary>
        public const double PointsPerInch = 72;

        /// <summary>
        /// Trần bề rộng CHỮ KÝ TƯƠI theo tỉ lệ bề rộng trang. Chữ ký tươi được co giãn (khác con dấu), nhưng
        /// ảnh scan có thể rất lớn nên phải có trần, nếu không chữ ký chiếm hết nửa trang.
        /// CON DẤU KHÔNG áp trần này - dấu luôn giữ nguyên kích thước gốc.
        /// </summary>
        public const double MaxChuKyTuoiWidthRatio = 0.25;

        // ===== Kẹp trong trang =====
        /// <summary>
        /// Lề tối thiểu giữa khối ảnh và mép trang, tính bằng point. Khối tràn mép thì DỊCH VÀO trong trang,
        /// tuyệt đối không thu nhỏ: dịch chỗ thì con dấu vẫn đúng cỡ, thu nhỏ là làm sai con dấu.
        /// </summary>
        public const double PageMargin = 8;
    }
}
