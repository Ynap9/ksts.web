namespace ksts.be.shared.Constants.GiayBao
{
    /// <summary>
    /// Danh mục loại trúng tuyển, chép nguyên văn từ bản dựng thiết kế
    /// (<c>print_giaybaotrungtuyen.html</c>, khối <c>ADMISSION_TEMPLATES</c>).
    ///
    /// Mã loại trúng tuyển quyết định BA thứ trên tờ giấy: loại bằng cấp, niên khóa và câu phương thức xét
    /// tuyển — kèm việc ẩn hiện mấy dòng chỉ có ở một nhóm mã. Đây là chỗ duy nhất mô tả quan hệ đó; sửa câu
    /// chữ phải sửa ở đây chứ không rải vào mẫu HTML hay service.
    /// </summary>
    public static class LoaiTrungTuyenConstants
    {
        /// <summary>
        /// Ba dạng bố cục trang 1, đúng theo ba nhóm file mẫu của phòng đào tạo.
        /// </summary>
        public const string BoCucDayDu = "full";
        public const string BoCucDuBi = "dubi";
        public const string BoCucTuyenThang = "tuyenthang";

        /// <summary>
        /// Các thẻ do mã loại trúng tuyển quyết định. Ô "Loại bằng cấp" bỏ trống hay ghi mã lạ thì để trống
        /// hết chỗ này - KHÔNG lùi về mã mặc định như bản dựng thiết kế: đoán sai sinh ra tờ giấy trông
        /// hoàn chỉnh nhưng sai loại bằng và sai phương thức, chỗ trống thì người soát thấy ngay.
        /// </summary>
        public static readonly string[] IdTheoLoai =
        [
            GiayBaoConstants.IdDegree,
            GiayBaoConstants.IdCohort,
            GiayBaoConstants.IdLead,
            GiayBaoConstants.IdMethodLabel,
            GiayBaoConstants.IdMethod,
        ];

        /// <summary>Loại bằng cấp: câu chữ in trên giấy và số năm đào tạo để suy ra niên khóa.</summary>
        public sealed record BangCap(string Ten, int SoNam);

        /// <summary>Một mã loại trúng tuyển: bằng cấp, bố cục trang, và câu phương thức xét tuyển.</summary>
        public sealed record LoaiTrungTuyen(string MaBang, string BoCuc, string PhuongThuc);

        /// <summary>Chỗ thay năm tuyển sinh trong câu phương thức.</summary>
        public const string ChoThayNam = "{nam}";

        public static readonly IReadOnlyDictionary<string, BangCap> BangCaps = new Dictionary<string, BangCap>
        {
            ["KS"] = new("Kỹ sư (chuyên sâu bậc 7, Khung trình độ quốc gia Việt Nam)", 5),
            ["KTS"] = new("Kiến trúc sư (chuyên sâu bậc 7, Khung trình độ quốc gia Việt Nam)", 5),
            ["CN"] = new("Cử nhân (trình độ bậc 6, Khung trình độ quốc gia Việt Nam)", 4),
        };

        public static readonly IReadOnlyDictionary<string, LoaiTrungTuyen> DanhMuc =
            new Dictionary<string, LoaiTrungTuyen>
            {
                ["100_CN"] = new("CN", BoCucDayDu, "Xét kết quả thi tốt nghiệp THPT năm {nam}"),
                ["100_CN_KT"] = new("CN", BoCucDayDu,
                    "Xét kết quả thi tốt nghiệp THPT và kết quả thi môn Năng khiếu năm {nam}"),
                ["100_KS"] = new("KS", BoCucDayDu, "Xét kết quả thi tốt nghiệp THPT năm {nam}"),
                ["100_KTS"] = new("KTS", BoCucDayDu,
                    "Xét kết quả thi tốt nghiệp THPT và kết quả thi môn Năng khiếu năm {nam}"),
                ["200_CN"] = new("CN", BoCucDayDu, "Xét kết quả học tập cấp THPT (Học bạ)"),
                ["200_KS"] = new("KS", BoCucDayDu, "Xét kết quả học tập cấp THPT (Học bạ)"),
                ["402_SPT_CN"] = new("CN", BoCucDayDu,
                    "Xét kết quả thi đánh giá năng lực do Trường Đại học Sư phạm Hà Nội tổ chức năm {nam}"),
                ["402_SPT_KS"] = new("KS", BoCucDayDu,
                    "Xét kết quả thi đánh giá năng lực do Trường Đại học Sư phạm Hà Nội tổ chức năm {nam}"),
                ["402_TSA_CN"] = new("CN", BoCucDayDu,
                    "Xét kết quả thi Đánh giá tư duy do Đại học Bách khoa Hà Nội chủ trì tổ chức"),
                ["402_TSA_KS"] = new("KS", BoCucDayDu,
                    "Xét kết quả thi Đánh giá tư duy do Đại học Bách khoa Hà Nội chủ trì tổ chức"),
                ["417_VSAT_CN"] = new("CN", BoCucDayDu,
                    "Xét kết quả thi đánh giá đầu vào đại học trên máy tính (V-SAT) năm {nam}"),
                ["417_VSAT_KS"] = new("KS", BoCucDayDu,
                    "Xét kết quả thi đánh giá đầu vào đại học trên máy tính (V-SAT) năm {nam}"),
                ["DBĐH_CN"] = new("CN", BoCucDuBi, "Diện học sinh dự bị đại học chuyển về"),
                ["DBĐH_KS"] = new("KS", BoCucDuBi, "Diện học sinh dự bị đại học chuyển về"),
                ["TuyenThang_CN"] = new("CN", BoCucTuyenThang, "Tuyển thẳng"),
                ["TuyenThang_KS"] = new("KS", BoCucTuyenThang, "Tuyển thẳng"),
                ["TuyenThang_KTS"] = new("KTS", BoCucTuyenThang, "Tuyển thẳng"),
            };

        // Câu mở đầu và vế nối, viết khác nhau theo từng nhóm mã đúng như bản .doc gốc của phòng đào tạo.
        public const string CauMoDayDu = "Thí sinh đã trúng tuyển ngành/chuyên ngành: ";
        public const string CauMoDuBi = "Thí sinh đủ điều kiện trúng tuyển vào học ngành/chuyên ngành: ";
        public const string CauMoTuyenThang =
            "Thí sinh trúng tuyển theo phương thức: Tuyển thẳng vào học ngành/chuyên ngành: ";

        public const string VeNoiDayDu = " theo phương thức: ";
        public const string VeNoiDuBi = " theo diện ";
        public const string VeNoiTuyenThang = "";

        public const string PhuongThucDuBi = "học sinh dự bị đại học chuyển về";
        public const string PhuongThucTuyenThang = "";

        // Thủ tục ở trang 2 khác nhau giữa nhóm dự bị và các nhóm còn lại.
        public const string TieuDeThuTucChinhQuy = "Hồ sơ thí sinh phải nộp khi đến nhập học";
        public const string TieuDeThuTucDuBi = "Hồ sơ dành cho học sinh dự bị đại học";
    }
}
