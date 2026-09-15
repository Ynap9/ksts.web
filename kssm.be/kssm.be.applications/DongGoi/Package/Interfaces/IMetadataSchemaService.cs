using kssm.be.applications.DongGoi.Package.Dtos;
using kssm.be.shared.Constants.DongGoi;

namespace kssm.be.applications.DongGoi.Package.Interfaces
{
    public interface IMetadataSchemaService
    {
        Task<PackageSchemaDto> GetAsync(PackageType packageType, MetadataObjectType objectType,
            CancellationToken cancellationToken = default);
    }
}
