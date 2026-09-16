namespace kssm.be.shared.Constants.LoKy
{
    /// <summary>Hằng số cho lô ký số hàng loạt.</summary>
    public static class LoKyConstants
    {
        // Tiền tố object key của lô nhận file TẢI LÊN, trên kho mặc định của service. Tách theo loKyId để
        // xoá lô là xoá gọn cả thư mục, và để file của hai lô không bao giờ đụng nhau.
        public const string ObjectKeyPrefix = "lo-ky";
        public const string SourceFolder = "nguon";

        // Object key do SERVER đặt, KHÔNG lấy tên file người dùng tải lên: hai người cùng tải "A.pdf" sẽ ghi
        // đè file của nhau, mà tên file người dùng còn có thể chứa ký tự phá đường dẫn.
        public const string SourceObjectKeyFormat = "{0}/{1}/{2}/{3:D6}.pdf";

        // Tên thư mục trên Drive khi lô tải lên không kèm tên thư mục nào — người dùng chọn từng file lẻ.
        public const string DefaultFolderNameFormat = "Lo ky {0} - {1:yyyyMMdd-HHmm}";

        /// <summary>Tiền tố chứa toàn bộ thư mục làm việc của một lô trên kho mặc định.</summary>
        public static string GetLoPrefix(int loKyId) => $"{ObjectKeyPrefix}/{loKyId}/";

        /// <summary>Tiền tố chứa bản nguồn tải lên của một lô.</summary>
        public static string GetSourcePrefix(int loKyId) => $"{ObjectKeyPrefix}/{loKyId}/{SourceFolder}/";

        /// <summary>Object key của một bản nguồn tải lên, đánh số theo thứ tự trong lô.</summary>
        public static string GetSourceObjectKey(int loKyId, int thuTu) =>
            string.Format(SourceObjectKeyFormat, ObjectKeyPrefix, loKyId, SourceFolder, thuTu);

        /// <summary>Tên thư mục mặc định trên Drive của lô nhận file tải lên.</summary>
        public static string GetDefaultFolderName(int loKyId, DateTime thoiDiem) =>
            string.Format(DefaultFolderNameFormat, loKyId, thoiDiem);

        public const string PdfContentType = "application/pdf";

        // Chỉ nhận PDF: luồng ký chỉ dựng được bản ký nối cho PDF.
        public const string PdfExtension = ".pdf";

        // Số file tối đa nhận trong MỘT đợt tải lên. Chặn ở đây để một request lỗi không kéo theo cả GB nằm
        // trong bộ nhớ server.
        public const int MaxFilesPerBatch = 200;

        // Số file ký ĐỒNG THỜI trong một lô. Đo thực ở KSTS: phần mật mã chỉ vài ms mỗi file, còn tải lên và
        // tải xuống kho object mới là chỗ tốn thời gian - mà đó là chờ MẠNG, nên số luồng phải lớn hơn số
        // nhân CPU. Chặn ở 8 vì 16 luồng đo ra CHẬM hơn: đường truyền tới kho đã bão hoà.
        public const int ParallelFiles = 8;

        public const int ParallelFinishingFiles = 8;

        /// <summary>
        /// Số file vừa ký xong kèm theo mỗi nhịp hỏi tiến độ. Có trần vì kèm cả nghìn dòng mỗi nhịp là thứ
        /// làm trình duyệt cạn tài nguyên rồi chết giữa lô.
        /// </summary>
        public const int RecentDonePerPoll = 100;

        // Mã trạng thái file gửi cho bên gọi. Gửi CHUỖI chứ không phải số thứ tự enum: bảng trạng thái trên
        // màn hình đọc thẳng giá trị này, mà số trần thì mỗi lần sửa enum là hiển thị sai lặng lẽ.
        public const string StatusPending = "cho";
        public const string StatusSigning = "dangKy";
        public const string StatusDone = "xong";
        public const string StatusFailed = "loi";
    }
}
