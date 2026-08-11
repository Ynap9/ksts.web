using ksts.be.shared.Constants.GiayBao;

namespace ksts.be.shared.Constants.LoKy
{
    /// <summary>Hằng số cho lô ký số hàng loạt.</summary>
    public static class LoKyConstants
    {
        // Tiền tố object key trên MinIO. Tách theo loKyId để xoá lô là xoá gọn cả thư mục, và để file của hai
        // lô không bao giờ đụng nhau.
        public const string ObjectKeyPrefix = "lo-ky";
        public const string ThuMucNguon = "nguon";

        // Object key do SERVER đặt, KHÔNG lấy tên file người dùng tải lên: hai người cùng tải "A.pdf" sẽ ghi
        // đè file của nhau, mà tên file người dùng còn có thể chứa ký tự phá đường dẫn.
        public const string DinhDangObjectKey = "{0}/{1}/{2}/{3:D6}.pdf";

        // Thư mục nhận bản ĐÃ KÝ khi người dùng bấm đẩy lên kho. Nằm cạnh thư mục bản chưa ký của CÙNG một
        // khoá, chỉ khác đoạn cuối — đây là nơi các hệ thống khác vào lấy giấy báo đã ký, còn lo-ky/ chỉ là
        // chỗ làm việc nội bộ của lô.
        //   GiayBaoTrungTuyen/{khoá}/GiayBaoTrungTuyen        <- bản chưa ký, do khâu import đẩy lên
        //   GiayBaoTrungTuyen/{khoá}/GiayBaoTrungTuyenDaKySo  <- bản đã ký
        public const string ThuMucDaKyTrenKho = "GiayBaoTrungTuyenDaKySo";

        /// <summary>Thư mục chứa bản đã ký của khoá hiện tại.</summary>
        public static string GetKhoDaKyPrefix() =>
            $"{GiayBaoConstants.KhoKeyPrefix}/{GiayBaoConstants.Khoa}/{ThuMucDaKyTrenKho}/";

        /// <summary>
        /// Object key bản đã ký. Tên file giữ nguyên tên bản chưa ký (số CCCD) nên bản ký và bản gốc của
        /// cùng một thí sinh nằm đúng cùng một tên ở hai thư mục, đối chiếu được ngay.
        /// </summary>
        public static string GetKhoDaKyKey(string tenFile) => GetKhoDaKyPrefix() + tenFile;

        /// <summary>
        /// Số file vừa ký xong kèm theo mỗi nhịp hỏi tiến độ, để bảng điền dần thời gian ký và dấu thời gian.
        /// Có trần vì kèm cả nghìn dòng mỗi nhịp là thứ làm trình duyệt cạn tài nguyên rồi chết giữa lô; lấy
        /// dư so với số file ký xong trong một nhịp nên thực tế không bao giờ chạm trần.
        /// </summary>
        public const int SoFileVuaXongMoiNhip = 100;

        /// <summary>
        /// Số file tải trước từ kho khi đóng gói. Nén phải ghi TUẦN TỰ vào luồng gửi cho trình duyệt, nhưng
        /// tải về là việc chờ mạng: giữ sẵn vài file đã tải xong thì ghi xong file này là có ngay file kế
        /// tiếp. Giữ mức thấp vì mỗi file chờ sẵn là một bản PDF nằm trong bộ nhớ.
        /// </summary>
        public const int SoFileTaiTruocKhiNen = 8;

        public const string PdfContentType = "application/pdf";
        public const string ZipContentType = "application/zip";

        // Chỉ nhận PDF: luồng ký chỉ dựng được bản ký nối cho PDF.
        public const string PhanMoRongPdf = ".pdf";

        // Số file tối đa nhận trong MỘT đợt upload. FE đang gửi 50; chặn ở đây để một request lỗi không kéo
        // theo cả GB nằm trong bộ nhớ server.
        public const int SoFileToiDaMoiDot = 200;

        // Số file ký ĐỒNG THỜI trong một lô. Đo thực: phần mật mã chỉ 6 ms/file và TSA 15 ms, còn tải lên và
        // tải xuống MinIO mới là chỗ tốn thời gian - mà đó là chờ MẠNG, nên số luồng phải lớn hơn số nhân CPU
        // chứ không bằng. Chặn ở 8 để không dội quá nhiều kết nối vào MinIO lẫn TSA cùng lúc.
        public const int SoFileSongSong = 8;

        // Độ dài token tải zip. Trình duyệt điều hướng thẳng tới đường tải nên KHÔNG gắn được header
        // Authorization; token này là thứ duy nhất chặn đường đó, phải đủ dài để không đoán được.
        public const int SoByteTaiToken = 32;

        // Mã trạng thái file gửi cho FE. Gửi CHUỖI chứ không phải số thứ tự enum: bảng trạng thái trên màn
        // hình đọc thẳng giá trị này, mà số trần thì mỗi lần sửa enum là FE hiển thị sai lặng lẽ.
        public const string MaTrangThaiCho = "cho";
        public const string MaTrangThaiDangKy = "dangKy";
        public const string MaTrangThaiXong = "xong";
        public const string MaTrangThaiLoi = "loi";
    }
}
