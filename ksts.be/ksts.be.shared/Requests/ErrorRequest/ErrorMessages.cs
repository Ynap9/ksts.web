using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ksts.be.shared.Requests.ErrorRequest
{
    public static class ErrorMessages
    {
        private static readonly Dictionary<int, string> _messages = new()
        {
            { ErrorCodes.System, "Lỗi hệ thống" },
            { ErrorCodes.InternalServerError, "Lỗi server" },
            { ErrorCodes.BadRequest, "Request không hợp lệ" },
            { ErrorCodes.NotFound, "Không tìm thấy trong hệ thống" },
            { ErrorCodes.Found, "Đã tồn tại trong hệ thống" },

            { ErrorCodes.TemplateNotFound, "Không tìm thấy template chữ ký" },
            { ErrorCodes.TemplateNameDuplicated, "Tên template đã được sử dụng" },
            { ErrorCodes.TemplateImageInvalid, "Ảnh không hợp lệ" },
            { ErrorCodes.TemplateAccessDenied, "Không có quyền truy cập template này" },
            { ErrorCodes.TemplatePositionInvalid, "Toạ độ khối trên trang không hợp lệ" },

            { ErrorCodes.CertificateNotFound, "Không tìm thấy chứng thư số" },
            { ErrorCodes.CertificateCannotSign, "Chứng thư số không đủ điều kiện để ký" },
            { ErrorCodes.CertificateStoreUnavailable, "Không đọc được kho chứng thư số" },

            { ErrorCodes.StorageNotConfigured, "Chưa cấu hình kho lưu trữ file" },
            { ErrorCodes.StorageUploadFailed, "Tải file lên kho lưu trữ thất bại" },
            { ErrorCodes.StorageDeleteFailed, "Xoá file trên kho lưu trữ thất bại" },

            { ErrorCodes.SealAnchorNotFound, "Không tìm thấy vị trí đặt con dấu trên tài liệu" },
            { ErrorCodes.SealImageUnreadable, "Không đọc được kích thước ảnh" },
            { ErrorCodes.SealSampleFileMissing, "Không tìm thấy file PDF mẫu" },

            { ErrorCodes.TimestampFailed, "Không lấy được dấu thời gian từ cơ quan cấp dấu" },
            { ErrorCodes.PdfPrepareFailed, "Không dựng được bản ký cho file PDF" },
            { ErrorCodes.SignatureAssembleFailed, "Không ghép được khối chữ ký số" },
            { ErrorCodes.PdfStructureUnsupported, "Cấu trúc file PDF chưa hỗ trợ ký số" },
            { ErrorCodes.PdfEncrypted, "File PDF được mã hoá nên không ký số được" },
            { ErrorCodes.PdfLocked, "File PDF bị khoá không cho ký thêm" },
            { ErrorCodes.SignatureTooLarge, "Khối chữ ký vượt quá chỗ trống đã chừa trong file" },
            { ErrorCodes.AppearanceBuildFailed, "Không dựng được khối chữ ký hiển thị" },

            { ErrorCodes.LoKyNotFound, "Không tìm thấy lô ký" },
            { ErrorCodes.LoKyAccessDenied, "Không có quyền truy cập lô ký này" },
            { ErrorCodes.LoKyDangChay, "Lô ký đang chạy" },
            { ErrorCodes.LoKyRong, "Lô ký chưa có file nào" },
            { ErrorCodes.StorageDownloadFailed, "Tải file từ kho lưu trữ thất bại" },
        };

        public static string GetMessage(int code)
        {
            return _messages.TryGetValue(code, out var message) ? message : "Unknown error.";
        }
    }
}
