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

        public string? DuongDanZip { get; set; }

        public long DungLuong { get; set; }

        public DateTime HetHanUtc { get; set; }

        // ===== Đẩy lên kho object =====
        // Tách hẳn bộ đếm khỏi khâu dựng: đẩy lên kho và tải zip về là hai việc độc lập, người dùng có thể
        // chỉ làm một trong hai, hoặc làm cả hai theo thứ tự bất kỳ sau khi lô đã dựng xong.
        public bool DangDayLenKho { get; set; }

        public int DaDayLenKho { get; set; }

        public int SoLoiDayLenKho { get; set; }

        public bool HoanTatDayLenKho { get; set; }

        public string? LoiDayLenKho { get; set; }

        public string? TienToKho { get; set; }
    }
}
