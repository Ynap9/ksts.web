namespace kssm.be.external.KySo.Signing.Dtos
{
    public class YeuCauKyDto
    {
        public string YeuCauId { get; set; } = string.Empty;

        public string DuLieuBase64 { get; set; } = string.Empty;
    }

    public class KetQuaKyDto
    {
        public string YeuCauId { get; set; } = string.Empty;

        public string? ChuKyBase64 { get; set; }

        public string? Loi { get; set; }

        /// <summary>
        /// Bật khi máy người dùng báo PHIÊN KÝ đã mất (rút token, plugin khởi động lại, phiên hết hạn).
        /// Khác hẳn lỗi của một file: cả lô phải dừng lại chờ mở phiên mới, chứ không đếm từng file thành lỗi.
        /// </summary>
        public bool PhienDaMat { get; set; }
    }
}
