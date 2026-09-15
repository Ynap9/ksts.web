using kssm.be.domain.DongGoi;

namespace kssm.be.applications.DongGoi.Package.Dtos
{
    public class NhomHoSoDto
    {
        public string Khoa { get; set; } = string.Empty;

        public string? FileCode { get; set; }

        public string? ExcelObjectKey { get; set; }

        public string TenHienThi { get; set; } = string.Empty;

        public PackageDossier? HoSo { get; set; }

        public List<PackageDocument> Files { get; } = new();

        public List<string> Loi { get; } = new();
    }
}
