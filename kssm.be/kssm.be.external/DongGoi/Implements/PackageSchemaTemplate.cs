using kssm.be.external.DongGoi.Dtos;
using kssm.be.external.DongGoi.Interfaces;
using kssm.be.shared.Constants.DongGoi;
using kssm.be.shared.Requests.AppException;
using kssm.be.shared.Requests.ErrorRequest;

namespace kssm.be.external.DongGoi.Implements
{
    public class PackageSchemaTemplate : IPackageSchemaTemplate
    {
        public List<PackEntryDto> Load()
        {
            var thuMuc = Path.Combine(AppContext.BaseDirectory, SipPackConstants.SchemaTemplateDir,
                SipPackConstants.SchemaTemplateSubDir);

            if (!Directory.Exists(thuMuc))
            {
                throw new UserFriendlyException(ErrorCodes.DongGoiThieuSchema,
                    $"Không tìm thấy thư mục schema \"{thuMuc}\" đi kèm bản build.");
            }

            var tep = Directory.GetFiles(thuMuc, "*.xsd").OrderBy(x => x).ToList();

            if (tep.Count == 0)
            {
                throw new UserFriendlyException(ErrorCodes.DongGoiThieuSchema,
                    "Thư mục schema không có file .xsd nào.");
            }

            return tep.Select(x => new PackEntryDto
            {
                DuongDan = $"{SipPackConstants.SchemasDir}/{Path.GetFileName(x)}",
                NoiDung = File.ReadAllBytes(x),
            }).ToList();
        }
    }
}
