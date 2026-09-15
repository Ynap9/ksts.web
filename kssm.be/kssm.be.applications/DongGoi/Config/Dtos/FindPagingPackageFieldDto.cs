using kssm.be.shared.Constants.DongGoi;
using kssm.be.shared.Requests.BaseRequest;
using Microsoft.AspNetCore.Mvc;

namespace kssm.be.applications.DongGoi.Config.Dtos
{
    public class FindPagingPackageFieldDto : BaseRequestPagingDto
    {
        [FromQuery(Name = "packageType")]
        public PackageType PackageType { get; set; }

        [FromQuery(Name = "objectType")]
        public MetadataObjectType? ObjectType { get; set; }
    }
}
