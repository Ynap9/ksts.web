namespace kssm.be.shared.Constants.LoKy
{
    /// <summary>Hằng số cho lô ký số hàng loạt.</summary>
    public static class LoKyConstants
    {
        // Tiền tố object key của lô nhận file TẢI LÊN, trên kho mặc định của service. Tách theo loKyId để
        // xoá lô là xoá gọn cả thư mục, và để file của hai lô không bao giờ đụng nhau.
        public const string ObjectKeyPrefix = "lo-ky";
        public const string SourceFolder = "nguon";
        public const string SignedFolder = "da-ky";

        // Object key do SERVER đặt, KHÔNG lấy tên file người dùng tải lên: hai người cùng tải "A.pdf" sẽ ghi
        // đè file của nhau, mà tên file người dùng còn có thể chứa ký tự phá đường dẫn.
        public const string SourceObjectKeyFormat = "{0}/{1}/{2}/{3:D6}.pdf";

        // Thư mục nhận bản đã ký của lô theo DỰ ÁN, nằm trong chính bucket của dự án đó.
        public const string ProjectSignedFolder = "ky-so";

        /// <summary>Tiền tố chứa bản nguồn tải lên của một lô.</summary>
        public static string GetSourcePrefix(int loKyId) => $"{ObjectKeyPrefix}/{loKyId}/{SourceFolder}/";

        /// <summary>Object key của một bản nguồn tải lên, đánh số theo thứ tự trong lô.</summary>
        public static string GetSourceObjectKey(int loKyId, int thuTu) =>
            string.Format(SourceObjectKeyFormat, ObjectKeyPrefix, loKyId, SourceFolder, thuTu);

        /// <summary>Tiền tố chứa bản đã ký của lô nhận file tải lên.</summary>
        public static string GetSignedPrefix(int loKyId) => $"{ObjectKeyPrefix}/{loKyId}/{SignedFolder}/";

        /// <summary>
        /// Tiền tố chứa bản đã ký của một dự án, đặt theo TÊN dự án như đã chốt với bên gọi. Tên có dấu và
        /// có khoảng trắng nên mọi chỗ ghép key này vào URL đều phải encode.
        /// </summary>
        public static string GetProjectSignedPrefix(string tenDuAn) => $"{ProjectSignedFolder}/{tenDuAn}/";

        public const string PdfContentType = "application/pdf";
        public const string ZipContentType = "application/zip";

        // Chỉ nhận PDF: luồng ký chỉ dựng được bản ký nối cho PDF.
        public const string PdfExtension = ".pdf";

        // Số file tối đa nhận trong MỘT đợt tải lên. Chặn ở đây để một request lỗi không kéo theo cả GB nằm
        // trong bộ nhớ server.
        public const int MaxFilesPerBatch = 200;

        // Số file ký ĐỒNG THỜI trong một lô. Đo thực ở KSTS: phần mật mã chỉ vài ms mỗi file, còn tải lên và
        // tải xuống kho object mới là chỗ tốn thời gian - mà đó là chờ MẠNG, nên số luồng phải lớn hơn số
        // nhân CPU. Chặn ở 8 vì 16 luồng đo ra CHẬM hơn: đường truyền tới kho đã bão hoà.
        public const int ParallelFiles = 8;

        // Độ dài token tải zip. Trình duyệt điều hướng thẳng tới đường tải nên KHÔNG gắn được header
        // Authorization; token này là thứ duy nhất chặn đường đó, phải đủ dài để không đoán được.
        public const int DownloadTokenBytes = 32;

        /// <summary>
        /// Số file vừa ký xong kèm theo mỗi nhịp hỏi tiến độ. Có trần vì kèm cả nghìn dòng mỗi nhịp là thứ
        /// làm trình duyệt cạn tài nguyên rồi chết giữa lô.
        /// </summary>
        public const int RecentDonePerPoll = 100;

        /// <summary>
        /// Số file tải trước từ kho khi đóng gói. Nén phải ghi TUẦN TỰ vào luồng gửi cho trình duyệt, nhưng
        /// tải về là việc chờ mạng: giữ sẵn vài file đã tải xong thì ghi xong file này là có ngay file kế
        /// tiếp. Giữ mức thấp vì mỗi file chờ sẵn là một bản PDF nằm trong bộ nhớ.
        /// </summary>
        public const int PrefetchWhenZipping = 8;

        // Mã trạng thái file gửi cho bên gọi. Gửi CHUỖI chứ không phải số thứ tự enum: bảng trạng thái trên
        // màn hình đọc thẳng giá trị này, mà số trần thì mỗi lần sửa enum là hiển thị sai lặng lẽ.
        public const string StatusPending = "cho";
        public const string StatusSigning = "dangKy";
        public const string StatusDone = "xong";
        public const string StatusFailed = "loi";
    }
}
