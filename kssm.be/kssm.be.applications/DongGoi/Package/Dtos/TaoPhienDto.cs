using kssm.be.shared.Constants.DongGoi;

namespace kssm.be.applications.DongGoi.Package.Dtos
{
    public class TaoPhienDto
    {
        public PackageType PackageType { get; set; }

        public List<MetadataObjectType> ObjectTypes { get; set; } = new();
    }
}
