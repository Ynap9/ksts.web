using kssm.be.applications.DongGoi.Package.Dtos;
using kssm.be.shared.Constants.DongGoi;

namespace kssm.be.applications.DongGoi.Package.Interfaces
{
    public interface IMetadataValueService
    {
        List<string> KiemDong(PackageSchemaDto schema, HeaderMatchReportDto baoCao,
            IReadOnlyDictionary<string, string> dong, PackageType packageType, string? maHoSo);

        string TachMa(string giaTri);
    }
}
