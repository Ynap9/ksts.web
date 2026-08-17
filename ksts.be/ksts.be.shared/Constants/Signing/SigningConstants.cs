namespace ksts.be.shared.Constants.Signing
{
    /// <summary>
    /// Hằng số cho luồng KÝ SỐ (lấy chứng thư số + ký PDF).
    /// Khác <see cref="SignatureConstants"/>: file này phục vụ việc TẠO chữ ký, file kia phục vụ việc KIỂM chữ ký.
    /// </summary>
    public static class SigningConstants
    {
        // ===== TSA (RFC 3161) =====
        // TSA Ban Cơ yếu Chính phủ. Đo thực: chỉ trả lời giao thức TSP (GET 403 / POST 400).
        public const string TsaUrl = "http://tsa.ca.gov.vn/";
        public const string TsaContentType = "application/timestamp-query";
        public const string TsaReplyContentType = "application/timestamp-reply";

        // Vòng gọi TSA đo thực mất 2s-131s -> timeout phải rộng, nếu không file hợp lệ bị đánh trượt oan.
        public const int TsaTimeoutSeconds = 180;

        // Số lần GỌI TSA cho một file (1 lần đầu + 2 lần thử lại). Ký cả lô là hàng nghìn lượt qua mạng công
        // cộng nên lỗi lẻ tẻ là chắc chắn có; thử lại cứu được phần lớn. Không nâng cao hơn: luồng ký theo
        // nguyên tắc fail-closed, thử mãi chỉ kéo dài lô mà vẫn phải báo hỏng.
        public const int TsaMaxAttempts = 3;

        // Giãn cách giữa hai lần thử, nhân đôi sau mỗi lần (2s rồi 4s). TSA hỏng thường do quá tải nhất thời,
        // thử lại ngay lập tức chỉ dội thêm vào đúng lúc nó đang nghẽn.
        public const int TsaRetryDelaySeconds = 2;

        // ===== OID =====
        // ESS signingCertificateV2 — .NET KHÔNG tự thêm, phải tự dựng bằng AsnWriter; thiếu nó thì file không
        // conform CAdES và Adobe/Foxit có thể từ chối dù verifier của mình vẫn nhận.
        public const string OidSigningCertificateV2 = "1.2.840.113549.1.9.16.2.47";

        // OID để tự dựng CMS bằng AsnWriter. Chữ ký do plugin ở máy người dùng ký rời nên không dùng được
        // SignedCms.ComputeSignature (nó tự ký tại chỗ) — phải ghép tay từng thành phần theo RFC 5652.
        public const string OidData = "1.2.840.113549.1.7.1";          // id-data: kiểu nội dung được ký
        public const string OidSignedData = "1.2.840.113549.1.7.2";    // id-signedData: bọc ngoài cùng
        public const string OidContentType = "1.2.840.113549.1.9.3";   // thuộc tính contentType
        public const string OidMessageDigest = "1.2.840.113549.1.9.4"; // thuộc tính messageDigest
        public const string OidSha256 = "2.16.840.1.101.3.4.2.1";
        // RSA PKCS#1 v1.5. Tham số PHẢI là NULL tường minh (RFC 3370), bỏ trống thì một số bộ đọc từ chối.
        public const string OidRsaEncryption = "1.2.840.113549.1.1.1";

        // ===== PDF signature dictionary =====
        // PAdES bắt buộc SubFilter này (đo thực trên gói mẫu: file thật ghi đúng giá trị này).
        public const string PdfSubFilter = "ETSI.CAdES.detached";
        public const string DefaultReason = "Ký số tài liệu";
        public const string DefaultLocation = "Việt Nam";

        // Chỗ trống chừa cho /Contents. ƯỚC LƯỢNG THỪA CÓ CHỦ ĐÍCH: kích thước CMS thật (chữ ký + cả chuỗi
        // chứng thư + token TSA) không đoán chính xác được trước khi ký. Chừa THIẾU là file hỏng và phải ký
        // lại từ đầu; chừa THỪA chỉ tốn vài KB mỗi file vì phần dư được đệm '0'.
        public const int SignaturePlaceholderBytes = 32768;

        // Bề rộng cố định cho mỗi số trong /ByteRange. Giá trị thật được vá ĐÈ LÊN chỗ trống sau khi đã biết
        // offset, nên độ dài PHẢI không đổi — dài thêm một byte là mọi offset vừa tính đã sai và file hỏng.
        // 10 chữ số đủ cho file tới ~10 GB.
        public const int ByteRangeFieldWidth = 10;

        // Định dạng ngày tháng của PDF cho /M: "D:YYYYMMDDHHmmSS+HH'mm'".
        public const string PdfDateFormat = "yyyyMMddHHmmss";

        // Việt Nam cố định UTC+7, KHÔNG có quy ước giờ mùa hè -> ghi thẳng offset thay vì tra cơ sở dữ liệu
        // múi giờ. Dùng offset của MÁY là sai khi server đặt múi giờ khác.
        public const int VietnamUtcOffsetHours = 7;

        // Tiền tố tên field chữ ký. Tên field phải DUY NHẤT trong tài liệu: trùng tên field cũ thì trình đọc
        // coi là cùng một ô và chữ ký mới ĐÈ CHẾT chữ ký cũ. Ghép thêm số hiệu object là chắc chắn không trùng.
        public const string SignatureFieldNamePrefix = "KSTS_Signature_";

        // Dấu hiệu nhận ra file ĐÃ có chữ ký, dùng để chặn ký lại khi template không bật cờ ký đè.
        // Bắt /ByteRange chứ không bắt /FT /Sig: ô chữ ký để trống cũng có /FT /Sig nhưng chưa ai ký, còn
        // /ByteRange chỉ xuất hiện trong dictionary chữ ký đã điền. Kèm /Type /Sig vì khoá /Type là TUỲ CHỌN
        // với dictionary chữ ký, file của bộ ký khác có thể lược đi.
        public const string SignatureValueMarker = @"/ByteRange\s*\[|/Type\s*/Sig\b";

        // ===== Hình thức chữ ký hiển thị =====
        // Số đo TRÍCH TỪ FILE THẬT, không phải tự chọn cho đẹp.
        // Khối 170x30pt; 2 dòng chữ: dòng 1 = CN người ký, dòng 2 = giờ ký ISO 8601.
        // Hai dòng được CANH GIỮA ô nên không có hằng số toạ độ từng dòng: chỗ đặt chữ suy ra từ chính
        // khổ ô mà người dùng kéo thả.
        public const double AppearanceWidth = 170;
        public const double AppearanceHeight = 30;
        public const string AppearanceFontFamily = "Times New Roman";
        public const double AppearanceFontSize = 10;

        // Giờ ký hiển thị trong khối chữ ký: ISO 8601 theo giờ VN, KHÔNG kèm phần bù múi giờ.
        // Mọi giờ trong khối đều là giờ VN nên phần bù chỉ làm khối dài thêm.
        public const string AppearanceTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffffff";

        // ===== Co giãn khối chữ ký theo khổ trang =====
        // Khổ trang mà bộ số 170x30pt ở trên được đo ra (A4 dọc). Khối chữ ký được nhân theo tỉ lệ
        // khổ trang thật / khổ tham chiếu này, nên trang A3 không bị chữ ký teo bằng con tem còn trang
        // nhỏ hơn A4 không bị tràn. Không có bước co giãn này thì một vị trí chọn 1 lần không thể dùng
        // chung cho cả thư mục có nhiều khổ giấy.
        public const double AppearanceReferencePageWidth = 595;
        public const double AppearanceReferencePageHeight = 842;

        // Sàn kích cỡ khi người dùng TỰ chọn vị trí: khối không được nhỏ hơn ngần này lần khổ chuẩn của trang.
        // Vị trí người dùng chọn được tôn trọng nguyên vẹn; chỉ hai thứ bị sửa là khối TRÀN ra ngoài trang
        // (kẹp lại) và khối NHỎ tới mức không đọc nổi chữ (kéo lên sàn này).
        public const double AppearanceMinScale = 0.5;

        // Lề chừa với mép trang khi TỰ đặt chữ ký (vị trí mặc định) — dính sát mép trông như lỗi in.
        // Cũng co giãn theo khổ trang như khối chữ ký.
        public const double AppearancePageMargin = 8;

        // ===== Tên file đã ký =====
        // File đã ký là bản MỚI, tên = "{tên gốc không đuôi}_daky_{yyyyMMdd_HHmmss}{đuôi gốc}".
        // Giữ nguyên file gốc để ký lỗi thì vẫn chạy lại được. Timestamp lấy MỘT LẦN đầu lô, dùng chung cả lô.
        public const string SignedFileSuffix = "_daky_";
        public const string SignedFileTimeFormat = "yyyyMMdd_HHmmss";

        // ===== Song song =====
        // Nghẽn nằm ở MẠNG (TSA) chứ không ở CPU -> ProcessorCount đơn thuần là con số sai.
        // Chặn dưới 2 để máy 1 nhân vẫn chạy; chặn trên 8 để không mở quá nhiều kết nối.
        public const int MinDegreeOfParallelism = 2;
        public const int MaxDegreeOfParallelism = 8;

        // Trần riêng cho tầng gọi TSA — fan-out không giới hạn là dội request vào TSA của cơ quan nhà nước.
        public const int MaxTsaConcurrency = 4;

        // Số byte giả dùng để test-sign khi kiểm tra token trước khi ký cả lô.
        public const int PreflightTestDataSize = 32;

        // Nhịp job giám sát token trong lúc ký. Đủ nhanh để dừng ngay khi người dùng rút token,
        // đủ thưa để không quét cert store liên tục.
        public const int TokenMonitorIntervalSeconds = 2;

        /// <summary>
        /// Chuỗi nhận diện provider LƯU KHÓA BẰNG PHẦN CỨNG (USB token/smartcard) trong tên CSP/KSP của private key.
        /// Cert trong store mà private key nằm ở provider phần cứng -> nguồn UsbToken; còn lại -> Local/Server.
        /// So khớp KHÔNG phân biệt hoa thường, dạng "chứa" — tên provider do middleware đặt nên không cố định
        /// (bit4id xPKI / VGCA PKI Manager, SafeNet, Gemalto...), liệt kê cứng từng tên đầy đủ là sẽ sót.
        /// </summary>
        public static readonly string[] HardwareKeyProviderMarkers =
        {
            "smart card",   // Microsoft Smart Card Key Storage Provider (đường chung của mọi minidriver)
            "bit4id",       // bit4id xPKI — middleware token Ban Cơ yếu
            "vgca",         // VGCA PKI Manager
            "token",
            "etoken",       // SafeNet eToken
            "safenet",
            "gemalto",
            "thales",
            "epass",        // Feitian ePass
            "feitian",
        };

        /// <summary>
        /// Nhà cung cấp CSP (CAPI đời cũ) mặc định của Windows cho khóa PHẦN MỀM — dùng để loại trừ.
        /// Cert dùng các provider này chắc chắn KHÔNG phải token.
        /// </summary>
        public static readonly string[] SoftwareKeyProviderMarkers =
        {
            "microsoft software key storage provider",
            "microsoft enhanced cryptographic provider",
            "microsoft base cryptographic provider",
            "microsoft strong cryptographic provider",
            "microsoft enhanced rsa and aes cryptographic provider",
        };
    }
}
