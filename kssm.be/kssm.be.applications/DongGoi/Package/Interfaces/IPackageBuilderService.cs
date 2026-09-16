using kssm.be.applications.DongGoi.Package.Dtos;
using kssm.be.domain.DongGoi;
using kssm.be.external.DongGoi.Dtos;

namespace kssm.be.applications.DongGoi.Package.Interfaces
{
    public interface IPackageBuilderService
    {
        Task<ViewTienDoDongGoiDto> DongGoiAsync(int sessionId, DongGoiDto dto,
            CancellationToken cancellationToken = default);

        Task<ViewTienDoDongGoiDto> TienDoDongGoiAsync(int sessionId, CancellationToken cancellationToken = default);

        Task<ViewDongGoiDto> DanhSachGoiAsync(int sessionId, CancellationToken cancellationToken = default);

        Task<ViewGoiDto> DungMotGoiAsync(PackageSession phien, string driveFolderId, DongGoiDto dto,
            PackageDossier hoSo, List<PackageDocument> cuaHoSo, ExcelDaDocDto capDto, PackageSchemaDto schemaHoSo,
            IReadOnlyList<PackageSchemaDto> schemaTaiLieu, IReadOnlyList<PackEntryDto> schemaEntries,
            CancellationToken cancellationToken);

        Task<ExcelDaDocDto> LaySheetAsync(Dictionary<string, ExcelDaDocDto> boNho, string objectKey,
            PackageSchemaDto schemaHoSo, IReadOnlyList<PackageSchemaDto> schemaTaiLieu,
            CancellationToken cancellationToken);
    }
}
