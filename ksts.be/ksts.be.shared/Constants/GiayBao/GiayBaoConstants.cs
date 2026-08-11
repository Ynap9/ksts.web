namespace ksts.be.shared.Constants.GiayBao
{
    /// <summary>
    /// Hằng số cho luồng IN GIẤY BÁO TRÚNG TUYỂN HÀNG LOẠT: tên cột Excel, id thẻ trên template HTML và
    /// bản đồ nối hai bên. Đây là chỗ duy nhất mô tả quan hệ cột - thẻ; đổi tên cột hay đổi template đều
    /// sửa tại đây chứ không rải vào service.
    /// </summary>
    public static class GiayBaoConstants
    {
        // ===== Tên cột trong file Excel =====
        public const string ColLoaiTrungTuyen = "Loại trúng tuyển";
        public const string ColHoTen = "Họ và Tên thí sinh";
        public const string ColCccd = "Số CCCD";
        public const string ColNgaySinh = "Ngày sinh";
        public const string ColHoKhau = "Hộ khẩu thường trú";
        public const string ColNganh = "Ngành / Chuyên ngành trúng tuyển";
        public const string ColKhuVuc = "Khu vực";
        public const string ColDoiTuongUuTien = "Đối tượng UT";
        public const string ColDiemCong = "Điểm cộng";
        public const string ColTruongDuBi = "Trường dự bị đại học";
        public const string ColDoiTuongTuyenThang = "Đối tượng tuyển thẳng";
        public const string ColSoVanBan = "Số văn bản";

        // ===== Tên cột của bản kết xuất theo mẫu Bộ =====
        // Cùng một trường nhưng mỗi đợt kết xuất đặt tên một khác. Giữ cả hai cách gọi thay vì bắt người
        // dùng sửa tiêu đề trong Excel: sửa tay trên file vài nghìn dòng là chỗ dễ sai và không ai kiểm được.
        public const string ColHoTenNgan = "Họ tên";
        public const string ColLoaiBangCap = "Loại bằng cấp";
        public const string ColNgayNhapHoc = "Ngày nhập học";
        public const string ColThoiGianNhapHoc = "Thời gian nhập học";
        public const string ColDinhDanhCaNhan = "Số ĐDCN";
        public const string ColKhuVucUuTien = "Khu vực ƯT";

        // ===== Id thẻ trên template =====
        public const string IdQrBox = "qr-box";
        public const string IdName = "doc-name";
        public const string IdCccd = "doc-cccd";
        public const string IdDob = "doc-dob";
        public const string IdResidence = "doc-residence";
        public const string IdMajor = "doc-major";
        public const string IdArea = "doc-area";
        public const string IdPriority = "doc-priority";
        public const string IdBonus = "doc-bonus";
        public const string IdDubiSchool = "doc-dubi-school";
        public const string IdTuyenThang = "doc-tuyenthang";
        public const string IdDocNo = "doc-no";

        // Các thẻ do MÃ LOẠI TRÚNG TUYỂN quyết định, không đọc thẳng từ một cột Excel nào.
        public const string IdDegree = "doc-degree";
        public const string IdCohort = "doc-cohort";
        public const string IdLead = "doc-lead";
        public const string IdMethodLabel = "doc-method-label";
        public const string IdMethod = "doc-method";
        public const string IdProcSubtitle = "proc-subtitle";

        // Các khối chỉ hiện với một nhóm mã.
        public const string IdLineDuBiSchool = "line-dubi-school";
        public const string IdLineTuyenThang = "line-tuyenthang";
        public const string IdFieldBonus = "field-bonus";
        public const string IdProcChinhQuy = "proc-chinhquy";
        public const string IdProcDuBi = "proc-dubi";

        public const string IdDate = "doc-date";
        public const string IdTime = "doc-time";

        /// <summary>
        /// Bản đồ ID THẺ -> CÁC TÊN CỘT có thể gặp, cho những trường đổ thẳng không qua tính toán. Đối chiếu
        /// theo TÊN cột chứ không theo thứ tự, nên file đảo cột hay thừa cột vẫn nhồi đúng.
        ///
        /// Một thẻ nhận NHIỀU tên cột vì các đợt kết xuất đặt tên khác nhau cho cùng một trường; lấy tên nào
        /// có mặt trước trong danh sách. Riêng <see cref="ColLoaiTrungTuyen"/> không có ở đây vì nó quyết
        /// định bố cục trang chứ không đổ ra thẻ nào.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, string[]> IdTheToTenCot =
            new Dictionary<string, string[]>
            {
                [IdName] = [ColHoTen, ColHoTenNgan],
                [IdCccd] = [ColCccd, ColDinhDanhCaNhan],
                [IdDob] = [ColNgaySinh],
                [IdResidence] = [ColHoKhau],
                [IdMajor] = [ColNganh],
                [IdArea] = [ColKhuVuc, ColKhuVucUuTien],
                [IdPriority] = [ColDoiTuongUuTien],
                [IdBonus] = [ColDiemCong],
                [IdDubiSchool] = [ColTruongDuBi],
                [IdTuyenThang] = [ColDoiTuongTuyenThang],
                [IdDocNo] = [ColSoVanBan],
                [IdDate] = [ColNgayNhapHoc],
                [IdTime] = [ColThoiGianNhapHoc],
            };

        /// <summary>Các tên cột có thể chứa mã loại trúng tuyển; bản thiết kế gọi là "Loại trúng tuyển".</summary>
        public static string[] CotLoaiTrungTuyen() => [ColLoaiBangCap, ColLoaiTrungTuyen];

        /// <summary>
        /// Năm tuyển sinh, thay vào chỗ <c>{nam}</c> của câu phương thức và dùng để suy ra niên khóa.
        ///
        /// ⚠️ Phải đổi mỗi mùa tuyển sinh, ĐỒNG THỜI với <see cref="Khoa"/> và với năm ghi cứng trong mẫu
        /// HTML (Templates/html/giay-bao-trung-tuyen.html).
        /// </summary>
        public const int NamTuyenSinh = 2026;

        /// <summary>Cột bắt buộc: thiếu tên thì không xác định được giấy báo của ai nên bỏ qua cả dòng.</summary>
        public static string[] CotBatBuoc() => IdTheToTenCot[IdName];

        /// <summary>Các tên cột chứa số định danh — dùng đặt tên file và dựng mã QR.</summary>
        public static string[] CotDinhDanh() => IdTheToTenCot[IdCccd];

        /// <summary>Tên cột bắt buộc để nêu trong thông báo lỗi cho người dùng.</summary>
        public const string RequiredColumn = ColHoTen;

        // ===== Nguồn dữ liệu và đầu ra =====
        /// <summary>Template nằm cạnh bản build, là asset chỉ đọc nên resolve từ AppContext.BaseDirectory.</summary>
        public static string GetTemplatePath() =>
            Path.Combine(AppContext.BaseDirectory, "Templates", "html", "giay-bao-trung-tuyen.html");

        /// <summary>Địa chỉ trang tra cứu, ghép với số CCCD thành nội dung mã QR.</summary>
        public const string QrBaseUrl = "https://nhaphoc.huce.edu.vn/candidate/landing/";

        public const string ZipContentType = "application/zip";
        public const string PdfContentType = "application/pdf";
        public const string PdfExtension = ".pdf";

        // ===== Đẩy lô lên kho object =====
        /// <summary>
        /// Thư mục gốc chứa giấy báo trong bucket, rồi tách theo KHOÁ tuyển sinh: mỗi khoá một thư mục riêng
        /// nên giấy báo các năm không lẫn vào nhau và dọn theo năm chỉ là xoá một tiền tố.
        ///
        /// ⚠️ <see cref="Khoa"/> phải đổi mỗi mùa tuyển sinh, ĐỒNG THỜI với khoá ghi trong mẫu HTML
        /// (Templates/html/giay-bao-trung-tuyen.html — ô "KHÓA" của khung tên và dòng "Khóa 71").
        /// </summary>
        public const string KhoKeyPrefix = "GiayBaoTrungTuyen";
        public const string Khoa = "K71";

        /// <summary>Thư mục chứa bản CHƯA ký của khoá hiện tại.</summary>
        public static string GetKhoKeyPrefix() => $"{KhoKeyPrefix}/{Khoa}/{KhoKeyPrefix}/";

        /// <summary>
        /// Object key một giấy báo chưa ký. Giữ nguyên tên file trong lô vì tên đó do SERVER đặt ở khâu dựng
        /// (số CCCD) chứ không phải tên người dùng tải lên — cùng một thí sinh dựng lại thì ghi đè đúng chỗ.
        /// </summary>
        public static string GetKhoKey(string tenFile) => GetKhoKeyPrefix() + tenFile;

        /// <summary>
        /// Số giấy báo tải trước từ kho khi đóng gói file nén. Nén phải ghi TUẦN TỰ từng file vào luồng gửi
        /// cho trình duyệt, nhưng tải về là việc chờ mạng: tải trước vài file trong khi đang ghi file hiện
        /// tại thì không phải đợi trọn một vòng đi-về cho mỗi file.
        ///
        /// Giữ ở mức thấp vì mỗi file chờ sẵn là một bản PDF nằm trong bộ nhớ (~900 KB): 8 file là ~7 MB,
        /// nâng cao nữa chỉ đổi băng thông đã bão hoà lấy bộ nhớ.
        /// </summary>
        public const int SoFileTaiTruocKhiNen = 8;
    }
}
