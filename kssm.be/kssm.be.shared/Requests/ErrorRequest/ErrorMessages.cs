namespace kssm.be.shared.Requests.ErrorRequest
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

            { ErrorCodes.CertificateNotFound, "Không tìm thấy chứng thư số" },
            { ErrorCodes.CertificateCannotSign, "Chứng thư số không đủ điều kiện để ký" },
            { ErrorCodes.CertificateStoreUnavailable, "Không đọc được kho chứng thư số" },

            { ErrorCodes.PluginSetupMissing, "Bản cài thiếu bộ cài plugin ký số" },

            { ErrorCodes.TemplateNotFound, "Không tìm thấy template cấu hình chữ ký" },
            { ErrorCodes.TemplateNameDuplicated, "Tên template đã tồn tại" },
            { ErrorCodes.TemplateImageInvalid, "Ảnh dấu đỏ hoặc chữ ký tươi không hợp lệ" },
            { ErrorCodes.TemplateAccessDenied, "Không có quyền trên template này" },
            { ErrorCodes.TemplatePositionInvalid, "Toạ độ khối chữ ký nằm ngoài trang" },
            { ErrorCodes.TemplateSampleFileMissing, "Bản cài thiếu file PDF mẫu" },

            { ErrorCodes.TimestampFailed, "Không lấy được dấu thời gian từ TSA" },
            { ErrorCodes.PdfPrepareFailed, "Không dựng được bản ký cho file PDF" },
            { ErrorCodes.SignatureAssembleFailed, "Không ghép được chữ ký vào file PDF" },
            { ErrorCodes.PdfStructureUnsupported, "Cấu trúc file PDF không hỗ trợ ký số" },
            { ErrorCodes.PdfEncrypted, "File PDF đang được mã hoá" },
            { ErrorCodes.PdfLocked, "File PDF đã bị khoá, không cho phép ký thêm" },
            { ErrorCodes.SignatureTooLarge, "Chữ ký vượt quá chỗ trống đã chừa trong file" },
            { ErrorCodes.AppearanceBuildFailed, "Không vẽ được khối chữ ký hiển thị" },
            { ErrorCodes.PdfAlreadySigned, "File đã có chữ ký số" },
            { ErrorCodes.ImageSizeUnreadable, "Không đọc được kích thước ảnh" },

            { ErrorCodes.LoKyNotFound, "Không tìm thấy lô ký" },
            { ErrorCodes.LoKyDangChay, "Lô ký đang chạy" },
            { ErrorCodes.LoKyRong, "Lô ký chưa có file nào" },
            { ErrorCodes.LoKyThuMucKhoRong, "Thư mục trên kho không có file PDF nào" },
            { ErrorCodes.LoKyPhienDaMat, "Phiên ký ở máy người dùng đã mất" },

            { ErrorCodes.DuAnNotFound, "Không tìm thấy dự án" },
            { ErrorCodes.DuAnChuaCauHinhMinio, "Dự án chưa cấu hình kho lưu trữ MinIO" },
            { ErrorCodes.DuAnKhongDungMinio, "Dự án không lưu file trên MinIO" },
            { ErrorCodes.DuAnSaiTrangThai, "Dự án không ở trạng thái cho phép ký số" },

            { ErrorCodes.StorageNotConfigured, "Chưa cấu hình kho lưu trữ file" },
            { ErrorCodes.StorageUploadFailed, "Không tải được file lên kho lưu trữ" },
            { ErrorCodes.StorageDeleteFailed, "Không xoá được file trên kho lưu trữ" },
            { ErrorCodes.StorageDownloadFailed, "Không tải được file từ kho lưu trữ" },
        };

        public static string GetMessage(int code)
        {
            return _messages.TryGetValue(code, out var message) ? message : "Unknown error.";
        }
    }
}
