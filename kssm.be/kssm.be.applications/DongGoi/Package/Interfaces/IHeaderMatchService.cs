using kssm.be.applications.DongGoi.Package.Dtos;

namespace kssm.be.applications.DongGoi.Package.Interfaces
{
    public interface IHeaderMatchService
    {
        HeaderMatchReportDto Match(PackageSchemaDto schema, IReadOnlyList<string> headers);

        string? ChonSheet(IReadOnlyList<string> sheetNames, string tenChuan);

        string? ChonHeader(HeaderMatchReportDto report, string fieldKey);
    }
}
