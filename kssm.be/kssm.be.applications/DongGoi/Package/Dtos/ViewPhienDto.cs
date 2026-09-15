using kssm.be.shared.Constants.DongGoi;

namespace kssm.be.applications.DongGoi.Package.Dtos
{
    public class ViewPhienDto
    {
        public int Id { get; set; }

        public PackageType PackageType { get; set; }

        public MetadataObjectType ObjectType { get; set; }

        public List<MetadataObjectType> DocumentTypes { get; set; } = new();

        public string RootFolderName { get; set; } = string.Empty;

        public bool Multi { get; set; }

        public string TrangThai { get; set; } = string.Empty;

        public int TongSo { get; set; }

        public int DaXong { get; set; }

        public int SoLoi { get; set; }

        public int SoCanhBao { get; set; }

        public int SoFileBoQua { get; set; }

        public DateTime? CreatedDate { get; set; }
    }
}
