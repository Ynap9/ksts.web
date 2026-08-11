namespace ksts.be.shared.Constants.Template
{
    /// <summary>
    /// Hằng số cho luồng TEMPLATE CẤU HÌNH CHỮ KÝ: ràng buộc ảnh dấu đỏ / chữ ký tươi, chỗ lưu ảnh, phân trang.
    /// Bản thân template nằm trong DB chính (KstsDbContext) nên ở đây không có hằng số về file DB.
    /// </summary>
    public static class TemplateConstants
    {
        // ===== Chỗ lưu ảnh =====
        /// <summary>
        /// Tiền tố object key trên MinIO, mỗi template một "thư mục" con theo Id.
        /// Ảnh phải được đẩy lên kho object rồi mới lưu URL vào template: web chạy nhiều instance hoặc trong
        /// container không có ổ đĩa bền vững, giữ file cạnh app là mất ảnh ngay lần deploy sau.
        /// </summary>
        public const string AssetKeyPrefix = "AnhDauVaChuKyTuoi";

        /// <summary>Tên object ảnh dấu đỏ, chưa gồm đuôi file.</summary>
        public const string DauDoObjectName = "dau-do";

        /// <summary>Tên object ảnh chữ ký tươi, chưa gồm đuôi file.</summary>
        public const string ChuKyTuoiObjectName = "chu-ky-tuoi";

        /// <summary>
        /// Object key của một ảnh thuộc template. Tách theo <paramref name="templateId"/> chứ KHÔNG dùng tên
        /// file người dùng tải lên: hai người cùng đặt tên "dau-do.png" sẽ ghi đè ảnh của nhau.
        /// </summary>
        public static string GetAssetKey(int templateId, string objectName, string extension) =>
            $"{AssetKeyPrefix}/{templateId}/{objectName}{extension}";

        /// <summary>Tiền tố chung của mọi ảnh thuộc một template - dùng để dọn sạch khi xoá template.</summary>
        public static string GetAssetKeyPrefix(int templateId) =>
            $"{AssetKeyPrefix}/{templateId}/";

        // ===== Ảnh =====
        /// <summary>Đuôi ảnh cho phép dùng làm dấu đỏ / chữ ký tươi.</summary>
        public static readonly string[] AllowedImageExtensions = { ".png", ".jpg", ".jpeg" };

        /// <summary>Trần dung lượng một ảnh (5 MB) - ảnh dấu/chữ ký chỉ vài chục KB, to hơn mức này là chọn nhầm file.</summary>
        public const long MaxImageBytes = 5 * 1024 * 1024;

        // ===== Độ đậm ảnh dấu đỏ / chữ ký tươi =====
        /// <summary>
        /// Độ đậm tính theo PHẦN TRĂM so với ảnh gốc: 100 là giữ nguyên, trên 100 đậm lên, dưới 100 nhạt đi.
        /// Sinh ra vì ảnh dấu và chữ ký tươi thường là bản chụp/quét trên nền giấy: nét mực ra xám nhạt chứ
        /// không đen, in ra nhìn mờ. Chỉnh ở template thay vì bắt người dùng sửa ảnh bằng công cụ ngoài.
        ///
        /// Mặc định 140 chứ không phải 100: đây là mức bản dựng thiết kế gốc đã cân cho ảnh chữ ký quét
        /// (contrast 1.4 kèm brightness 0.8), và ảnh quét gần như luôn cần đậm thêm mới ra nét như bản giấy.
        /// </summary>
        public const int DoDamMacDinh = 140;

        /// <summary>
        /// Sàn và trần độ đậm. Dưới 40% thì nét mực nhạt tới mức gần như mất hẳn; trên 250% thì phần nền
        /// mờ quanh nét chữ cũng bị kéo thành vệt bẩn, ảnh trông tệ hơn bản gốc.
        /// </summary>
        public const int DoDamMin = 40;
        public const int DoDamMax = 250;

        /// <summary>
        /// Độ đậm gồm HAI vế, không chỉ tăng tương phản: kéo tương phản với hệ số f = phần trăm / 100, ĐỒNG
        /// THỜI hạ độ sáng b = 1 - (f - 1) * <see cref="DoDamHeSoGiamSang"/>. Chỉ tăng tương phản thì nét mực
        /// tách khỏi nền rõ hơn nhưng KHÔNG sâu thêm, kéo hết thanh vẫn không ra nét chắc như bản giấy.
        /// Ở mức 140% hai vế ra đúng bộ số bản thiết kế đã cân: contrast 1.4 và brightness 0.8.
        /// </summary>
        public const double DoDamHeSoGiamSang = 0.5;

        /// <summary>
        /// Sàn độ sáng. Hạ quá mức này thì cả vùng nền mờ quanh nét cũng thành xám đậm, nhìn ra vệt bẩn chứ
        /// không phải chữ ký đậm.
        /// </summary>
        public const double DoDamSangToiThieu = 0.5;

        // ===== Độ dày nét chữ ký tươi =====
        /// <summary>
        /// Độ dày nét tính theo PHẦN TRĂM: 0 là để nguyên ảnh, 100 là mức bản dựng thiết kế gốc đã cân.
        ///
        /// Đây là vế thứ hai của "nét đậm", tách hẳn khỏi <see cref="DoDamMacDinh"/>: độ đậm chỉ ánh xạ lại
        /// độ sáng của TỪNG pixel tại chỗ nên nét mảnh vẫn mảnh, chỉ sẫm màu hơn. Muốn nét DÀY lên thì mực
        /// phải lan sang pixel bên cạnh — việc mà /Decode không làm được. Bản dựng gốc
        /// (print_giaybaotrungtuyen.html, rule .signature-sign) làm bằng hai lớp
        /// drop-shadow(0 0 0.35px) cùng màu mực chồng lên nhau.
        /// </summary>
        public const int DoDayNetMacDinh = 100;

        /// <summary>
        /// Sàn và trần độ dày nét. 0 là tắt hẳn, giữ đúng ảnh gốc. Trên 200% thì các nét nằm sát nhau trong
        /// chữ ký viết tay dính vào nhau thành mảng đặc, đọc ra vệt mực chứ không phải chữ ký.
        /// </summary>
        public const int DoDayNetMin = 0;
        public const int DoDayNetMax = 200;

        /// <summary>
        /// Bán kính nong nét ở mức 100%, tính theo TỈ LỆ so với bề rộng ảnh vẽ ra chứ không phải số điểm cố
        /// định: bản gốc cân 0,35px cho ảnh rộng 158px, giữ nguyên tỉ lệ đó thì ô to ô nhỏ đều ra nét dày
        /// tương đương nhau, còn số đo cứng sẽ thành nét mập ở ô nhỏ và mất tác dụng ở ô lớn.
        /// </summary>
        public const double DoDayNetBanKinhTiLe = 0.35 / 158d;

        /// <summary>
        /// Số hướng vẽ chồng quanh nét. Bốn hướng để lại vết hình chữ thập ở nét chéo; tám hướng đã tròn đều
        /// mà vẫn chỉ là tám lệnh vẽ trỏ về CÙNG một ảnh, không làm file phình thêm.
        /// </summary>
        public const int DoDayNetSoHuong = 8;

        // ===== File PDF mẫu =====
        /// <summary>
        /// Đường dẫn file PDF mẫu đi kèm bản build, để người dùng đặt thử vị trí khi chưa có hồ sơ thật.
        /// Là asset CHỈ ĐỌC nằm cạnh app nên resolve từ AppContext.BaseDirectory.
        /// </summary>
        public static string GetSamplePdfPath() =>
            Path.Combine(AppContext.BaseDirectory, "Templates", "sample", "file-mau-ky-so.pdf");

        // ===== Phân trang =====
        public const int DefaultPageNumber = 1;
        public const int DefaultPageSize = 10;
        public const int MaxPageSize = 200;
    }
}
