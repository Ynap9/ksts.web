namespace kssm.be.shared.Requests.ErrorRequest
{
    public static class ErrorCodes
    {
        //Các mã lỗi căn bản
        public const int System = 1;
        public const int BadRequest = 400;
        public const int NotFound = 404;
        public const int Found = 409;
        public const int InternalServerError = 500;

        //Chứng thư số: 1020 - 1039
        public const int CertificateNotFound = 1020;
        public const int CertificateCannotSign = 1021;
        public const int CertificateStoreUnavailable = 1022;

        //Plugin ký số ở máy người dùng: 1080 - 1099
        public const int PluginSetupMissing = 1080;


        //Lưu trữ file trên MinIO: 1040 - 1059
        public const int StorageNotConfigured = 1040;
        public const int StorageUploadFailed = 1041;
        public const int StorageDeleteFailed = 1042;
        public const int StorageDownloadFailed = 1043;


        //Ký số PDF: 1140 - 1159
        public const int TimestampFailed = 1140;
        public const int PdfPrepareFailed = 1141;
        public const int SignatureAssembleFailed = 1142;
        public const int PdfStructureUnsupported = 1143;
        public const int PdfEncrypted = 1144;
        public const int PdfLocked = 1145;
        public const int SignatureTooLarge = 1146;
        public const int AppearanceBuildFailed = 1147;
        public const int PdfAlreadySigned = 1148;
        public const int ImageSizeUnreadable = 1149;

        //Lô ký số hàng loạt: 1160 - 1179
        public const int LoKyNotFound = 1160;
        public const int LoKyDangChay = 1162;
        public const int LoKyRong = 1163;
        public const int LoKyThuMucKhoRong = 1164;
        public const int LoKyPhienDaMat = 1166;

        //Dự án bên sao_mai: 1180 - 1199
        public const int DuAnNotFound = 1180;
        public const int DuAnChuaCauHinhMinio = 1181;
        public const int DuAnKhongDungMinio = 1182;
        public const int DuAnSaiTrangThai = 1183;

        //Template cấu hình chữ ký: 1001 - 1019
        public const int TemplateNotFound = 1001;
        public const int TemplateNameDuplicated = 1002;
        public const int TemplateImageInvalid = 1003;
        public const int TemplateAccessDenied = 1004;
        public const int TemplatePositionInvalid = 1005;
        public const int TemplateSampleFileMissing = 1006;

    }
}
