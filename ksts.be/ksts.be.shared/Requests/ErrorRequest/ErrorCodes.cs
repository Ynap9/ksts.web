using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ksts.be.shared.Requests.ErrorRequest
{
    public static class ErrorCodes
    {
        //Các mã lỗi căn bản
        public const int System = 1;
        public const int BadRequest = 400;
        //public const int Unauthorized = 401;
        public const int NotFound = 404;
        public const int Found = 409;
        public const int InternalServerError = 500;

        public const int AuthInvalidPassword = 101;
        public const int AuthErrorCreateUser = 102;
        public const int AuthErrorUserNotFound = 103;
        public const int AuthErrorCreateRole = 104;
        public const int AuthErrorRoleNotFound = 105;
        public const int AuthErrorUserEmailHuceNotFound = 106;

        //Template cấu hình chữ ký: 1001 - 1019
        public const int TemplateNotFound = 1001;
        public const int TemplateNameDuplicated = 1002;
        public const int TemplateImageInvalid = 1003;
        public const int TemplateAccessDenied = 1004;
        public const int TemplatePositionInvalid = 1005;

        //Chứng thư số: 1020 - 1039
        public const int CertificateNotFound = 1020;
        public const int CertificateCannotSign = 1021;
        public const int CertificateStoreUnavailable = 1022;

        //Lưu trữ file trên MinIO: 1040 - 1059
        public const int StorageNotConfigured = 1040;
        public const int StorageUploadFailed = 1041;
        public const int StorageDeleteFailed = 1042;
        public const int StorageDownloadFailed = 1043;

        //Đặt con dấu và chữ ký tươi: 1060 - 1079
        public const int SealAnchorNotFound = 1060;
        public const int SealImageUnreadable = 1061;
        public const int SealSampleFileMissing = 1062;

        //Plugin ký số ở máy người dùng: 1080 - 1099
        public const int PluginSetupMissing = 1080;

        //Mã QR tra cứu trên giấy báo: 1100 - 1119
        public const int QrContentEmpty = 1100;
        public const int QrContentTooLong = 1101;

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

        //Lô ký số hàng loạt: 1160 - 1179
        public const int LoKyNotFound = 1160;
        public const int LoKyAccessDenied = 1161;
        public const int LoKyDangChay = 1162;
        public const int LoKyRong = 1163;
        public const int LoKyThuMucKhoRong = 1164;
        public const int LoKyDangDayLenKho = 1165;

        //In giấy báo trúng tuyển hàng loạt: 1120 - 1139
        public const int ExcelUnreadable = 1120;
        public const int ExcelMissingRequiredColumn = 1121;
        public const int ExcelNoValidRow = 1122;
        public const int GiayBaoTemplateMissing = 1123;
        public const int ConvertFileNotConfigured = 1124;
        public const int ConvertFileFailed = 1125;
        public const int GiayBaoJobNotFound = 1126;
        // 1127, 1128 đã dùng cho bước đẩy lô lên kho theo yêu cầu riêng — bước đó không còn nữa vì giấy báo
        // dựng xong là lên kho luôn. Không cấp lại hai số này cho nghiệp vụ khác.
    }
}
