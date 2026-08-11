using System.Text.Json.Serialization;

namespace ksts.be.external.Jobs.Dtos
{
    public class ZipJobDto
    {
        public string JobId { get; set; } = string.Empty;

        public string TaiToken { get; set; } = string.Empty;

        public int TongSo { get; set; }

        public int DaXong { get; set; }

        public int SoLoi { get; set; }

        public bool HoanTat { get; set; }

        public string? LoiChung { get; set; }

        public long DungLuong { get; set; }

        public DateTime HetHanUtc { get; set; }

        public string? TienToKho { get; set; }

        /// <summary>
        /// Dòng nào hỏng và vì sao. Chỉ chứa dòng LỖI nên số phần tử bằng số lỗi thật, không phải cả lô —
        /// bảng bên FE tra theo thứ tự dòng để hiện cột nguyên nhân.
        /// </summary>
        public List<DongLoiDto> DongLoi { get; set; } = [];

        /// <summary>
        /// Danh sách file đã lên kho, chỉ dùng ở khâu đóng gói phía máy chủ. KHÔNG gửi cho FE: nó dài bằng
        /// số giấy báo, mà FE hỏi tiến độ mỗi 2 giây — cuối lô 5000 file là mỗi nhịp kéo về ngót trăm KB
        /// danh sách không ai dùng tới.
        /// </summary>
        [JsonIgnore]
        public List<string> TenFileDaDay { get; set; } = [];
    }
}
