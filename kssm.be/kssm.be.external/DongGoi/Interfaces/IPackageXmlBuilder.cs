using kssm.be.external.DongGoi.Dtos;
using System.Xml.Linq;

namespace kssm.be.external.DongGoi.Interfaces
{
    public interface IPackageXmlBuilder
    {
        XDocument BuildRootMets(RootMetsInputDto input);

        XDocument BuildRepMets(RepMetsInputDto input);

        XElement BuildSimpleDc(IEnumerable<KeyValuePair<string, string>> truong);

        byte[] Serialize(XDocument doc);

        string NewUuid();

        string NewFileId();

        string FormatDateTime(DateTimeOffset value);

        string GetMimeType(string extension);
    }
}
