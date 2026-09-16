namespace kssm.be.shared.Constants.DongGoi
{
    public static class KiemTraConstants
    {
        public const string ObjectKeyPrefix = "dong-goi";
        public const string SourceFolder = "nguon";
        public const string SourceObjectKeyFormat = "{0}/{1}/{2}/{3:D6}{4}";

        public const string DefaultDriveFolderNameFormat = "Dong goi {0} - {1:yyyyMMdd-HHmm}";

        public static string GetSessionPrefix(int sessionId) => $"{ObjectKeyPrefix}/{sessionId}/";

        public static string GetSourcePrefix(int sessionId) =>
            $"{ObjectKeyPrefix}/{sessionId}/{SourceFolder}/";

        public static string GetSourceObjectKey(int sessionId, int thuTu, string extension) =>
            string.Format(SourceObjectKeyFormat, ObjectKeyPrefix, sessionId, SourceFolder, thuTu, extension);

        public static string GetDefaultDriveFolderName(int sessionId, DateTime thoiDiem) =>
            string.Format(DefaultDriveFolderNameFormat, sessionId, thoiDiem);

        public const string PdfExtension = ".pdf";
        public const string XlsxExtension = ".xlsx";
        public const string XlsLegacyExtension = ".xls";

        public const string PdfContentType = "application/pdf";
        public const string XlsxContentType =
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

        public const string DauPhanCachLoai = ",";
        public const string DauNoiMaTaiLieu = ".";

        public const int MaxFilesPerBatch = 200;
        public const int ParallelFiles = 8;
        public const int RecentDonePerPoll = 100;
        public const double NguongGhepTen = 0.8d;
        public const int DongTieuDe = 1;
        public const int SoGioGiuPhienMacDinh = 24;

        public const string StatusPending = "cho";
        public const string StatusRunning = "dangKiem";
        public const string StatusPassed = "dat";
        public const string StatusWarning = "canhBao";
        public const string StatusFailed = "loi";
        public const string StatusCancelled = "huy";
    }
}
