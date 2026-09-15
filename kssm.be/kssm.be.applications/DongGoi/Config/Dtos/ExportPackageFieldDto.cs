using kssm.be.shared.Constants.DongGoi;
using Microsoft.AspNetCore.Mvc;

namespace kssm.be.applications.DongGoi.Config.Dtos
{
    public class ExportPackageFieldDto
    {
        [FromQuery(Name = "packageType")]
        public PackageType PackageType { get; set; }

        [FromQuery(Name = "objectType")]
        public List<MetadataObjectType> ObjectType { get; set; } = new List<MetadataObjectType>();
    }
}
