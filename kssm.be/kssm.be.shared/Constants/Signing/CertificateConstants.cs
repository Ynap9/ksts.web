namespace kssm.be.shared.Constants.Signing
{
    /// <summary>
    /// Hằng số cho việc ĐỌC CHỨNG THƯ SỐ trong kho chứng thư: nhận diện khoá nằm trên token hay trong máy,
    /// và ký thử để biết token còn cắm. Khác <see cref="SignatureConstants"/>: file kia ghim CA để dựng chuỗi
    /// tin cậy.
    /// </summary>
    public static class CertificateConstants
    {
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

        // Số byte giả dùng để test-sign khi kiểm tra token trước khi ký cả lô.
        public const int PreflightTestDataSize = 32;

        // Nhịp job giám sát token trong lúc ký. Đủ nhanh để dừng ngay khi người dùng rút token,
        // đủ thưa để không quét cert store liên tục.
        public const int TokenMonitorIntervalSeconds = 2;
    }
}
