namespace kssm.be.external.DongGoi.Dtos
{
    public class RepMetsInputDto
    {
        public string MetsType { get; set; } = string.Empty;

        public string Created { get; set; } = string.Empty;

        public string? NguoiTao { get; set; }

        public string? MaPhong { get; set; }

        public string DataFileGrpId { get; set; } = string.Empty;

        public List<PackDocDto> Docs { get; set; } = new();
    }
}
