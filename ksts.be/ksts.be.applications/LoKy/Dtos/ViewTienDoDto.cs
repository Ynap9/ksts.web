namespace ksts.be.applications.LoKy.Dtos
{
    public class ViewTienDoDto
    {
        public int Id { get; set; }

        public string TrangThai { get; set; } = string.Empty;

        public string TaiToken { get; set; } = string.Empty;

        public int TongSo { get; set; }

        public int DaXong { get; set; }

        public int SoLoi { get; set; }

        public bool DangChay { get; set; }

        public bool HoanTat { get; set; }

        public string? LoiChung { get; set; }

        // Tiến độ đẩy bản đã ký lên kho — bộ đếm riêng, độc lập với tiến độ ký và với việc tải zip về.
        public bool DangDayLenKho { get; set; }

        public int DaDayLenKho { get; set; }

        public int SoLoiDayLenKho { get; set; }

        public bool HoanTatDayLenKho { get; set; }

        public string? LoiDayLenKho { get; set; }

        public string? TienToKho { get; set; }

        public List<ViewFileKyDto> Files { get; set; } = new();
    }
}
