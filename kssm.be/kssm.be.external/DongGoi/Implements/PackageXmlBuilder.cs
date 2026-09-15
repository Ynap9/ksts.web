using kssm.be.external.DongGoi.Dtos;
using kssm.be.external.DongGoi.Interfaces;
using kssm.be.shared.Constants.DongGoi;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace kssm.be.external.DongGoi.Implements
{
    public class PackageXmlBuilder : IPackageXmlBuilder
    {
        public static readonly XNamespace MetsNs = SipPackConstants.NsMets;
        public static readonly XNamespace XsiNs = SipPackConstants.NsXsi;
        public static readonly XNamespace XlinkNs = SipPackConstants.NsXlink;
        public static readonly XNamespace CsipNs = SipPackConstants.NsCsip;
        public static readonly XNamespace SipNs = SipPackConstants.NsSip;

        public XDocument BuildRootMets(RootMetsInputDto input)
        {
            var mets = CreateMetsRoot(input.Objid, input.Label, input.MetsType,
                SipPackConstants.SchemaLocationRoot);

            mets.Add(new XElement(MetsNs + "metsHdr",
                new XAttribute("CREATEDATE", input.Created),
                new XAttribute("LASTMODDATE", input.Created),
                new XAttribute("RECORDSTATUS", SipPackConstants.RecordStatusDefault),
                new XAttribute(CsipNs + "OAISPACKAGETYPE", SipPackConstants.OaisPackageType),
                new XElement(MetsNs + "agent",
                    new XAttribute("ROLE", SipPackConstants.AgentRole),
                    new XAttribute("TYPE", SipPackConstants.AgentTypeSoftware),
                    new XAttribute("OTHERTYPE", SipPackConstants.AgentOtherTypeSoftware),
                    new XElement(MetsNs + "name", SipPackConstants.AgentName),
                    new XElement(MetsNs + "note",
                        new XAttribute(CsipNs + "NOTETYPE", SipPackConstants.NoteTypeIdentificationCode),
                        input.MaPhong ?? string.Empty)),
                new XElement(MetsNs + "altRecordID",
                    new XAttribute("TYPE", SipPackConstants.AltRecordIdType),
                    input.FileCode)));

            var eadHref = $"{SipPackConstants.MetadataDir}/{SipPackConstants.DescriptiveDir}"
                + $"/{SipPackConstants.EadFileName}";

            mets.Add(BuildDmdSec(input.EadDmdSecId, input.EadMdRefId, eadHref, input.EadSize,
                input.EadChecksum, input.Created));

            mets.Add(new XElement(MetsNs + "amdSec", new XAttribute("ID", NewUuid())));

            var schemasGrp = new XElement(MetsNs + "fileGrp",
                new XAttribute("ID", input.SchemasFileGrpId),
                new XAttribute("USE", SipPackConstants.UseSchemas));

            foreach (var schema in input.Schemas)
            {
                schemasGrp.Add(BuildFileElement(schema, input.Created,
                    $"{SipPackConstants.SchemasDir}/{schema.FileName}"));
            }

            var repHref = $"{SipPackConstants.RepresentationsDir}/{SipPackConstants.Rep1Dir}"
                + $"/{SipPackConstants.MetsFileName}";

            var repGrp = new XElement(MetsNs + "fileGrp",
                new XAttribute("ID", input.RepFileGrpId),
                new XAttribute("USE", SipPackConstants.UseRepresentations),
                BuildFileElement(input.RepMets, input.Created, repHref));

            mets.Add(new XElement(MetsNs + "fileSec",
                new XAttribute("ID", NewUuid()),
                schemasGrp,
                repGrp));

            mets.Add(new XElement(MetsNs + "structMap",
                new XAttribute("ID", NewUuid()),
                new XAttribute("TYPE", SipPackConstants.StructMapTypePhysical),
                new XAttribute("LABEL", SipPackConstants.LabelCsip),
                new XElement(MetsNs + "div",
                    new XAttribute("ID", NewUuid()),
                    new XAttribute("LABEL", input.Objid),
                    new XElement(MetsNs + "div",
                        new XAttribute("ID", NewUuid()),
                        new XAttribute("DMDID", input.EadDmdSecId),
                        new XAttribute("LABEL", SipPackConstants.LabelMetadata)),
                    new XElement(MetsNs + "div",
                        new XAttribute("ID", NewUuid()),
                        new XAttribute("LABEL", SipPackConstants.UseSchemas),
                        new XElement(MetsNs + "fptr",
                            new XAttribute("FILEID", input.SchemasFileGrpId))),
                    new XElement(MetsNs + "div",
                        new XAttribute("ID", NewUuid()),
                        new XAttribute("LABEL", SipPackConstants.UseRepresentations),
                        new XElement(MetsNs + "mptr",
                            new XAttribute(XlinkNs + "type", SipPackConstants.XlinkTypeSimple),
                            new XAttribute(XlinkNs + "href", repHref),
                            new XAttribute(XlinkNs + "title", input.RepFileGrpId),
                            new XAttribute("LOCTYPE", SipPackConstants.LocType))))));

            return new XDocument(new XDeclaration("1.0", "UTF-8", "yes"), mets);
        }

        public XDocument BuildRepMets(RepMetsInputDto input)
        {
            var mets = CreateMetsRoot(SipPackConstants.RepObjid, string.Empty, input.MetsType,
                SipPackConstants.SchemaLocationRep);

            mets.Add(new XElement(MetsNs + "metsHdr",
                new XAttribute("CREATEDATE", input.Created),
                new XAttribute("LASTMODDATE", input.Created),
                new XAttribute("RECORDSTATUS", SipPackConstants.RecordStatusDefault),
                new XAttribute(CsipNs + "OAISPACKAGETYPE", SipPackConstants.OaisPackageType),
                new XElement(MetsNs + "agent",
                    new XAttribute("ROLE", SipPackConstants.AgentRole),
                    new XAttribute("TYPE", SipPackConstants.AgentTypeIndividual),
                    new XAttribute("OTHERTYPE", string.Empty),
                    new XElement(MetsNs + "name", input.NguoiTao ?? string.Empty),
                    new XElement(MetsNs + "note",
                        new XAttribute(CsipNs + "NOTETYPE", SipPackConstants.NoteTypeIdentificationCode),
                        input.MaPhong ?? string.Empty))));

            foreach (var doc in input.Docs)
            {
                var href = $"{SipPackConstants.MetadataDir}/{SipPackConstants.DescriptiveDir}"
                    + $"/{doc.Meta.FileName}";

                mets.Add(BuildDmdSec(doc.DmdSecId, doc.MdRefId, href, doc.Meta.Size, doc.Meta.Checksum,
                    input.Created));
            }

            mets.Add(new XElement(MetsNs + "amdSec", new XAttribute("ID", NewUuid())));

            var dataGrp = new XElement(MetsNs + "fileGrp",
                new XAttribute("ID", input.DataFileGrpId),
                new XAttribute("USE", SipPackConstants.UseData));

            foreach (var doc in input.Docs)
            {
                dataGrp.Add(BuildFileElement(doc.Data, input.Created,
                    $"{SipPackConstants.DataDir}/{doc.Data.FileName}"));
            }

            mets.Add(new XElement(MetsNs + "fileSec", new XAttribute("ID", NewUuid()), dataGrp));

            var metadataLink = new XElement(MetsNs + "div",
                new XAttribute("ID", NewUuid()),
                new XAttribute("LABEL", SipPackConstants.LabelMetadataLink));

            foreach (var doc in input.Docs)
            {
                metadataLink.Add(new XElement(MetsNs + "div",
                    new XAttribute("ID", NewUuid()),
                    new XAttribute("DMDID", doc.DmdSecId),
                    new XAttribute("LABEL", SipPackConstants.LabelMetadataLinkFile),
                    new XElement(MetsNs + "fptr", new XAttribute("FILEID", doc.Data.FileId))));
            }

            mets.Add(new XElement(MetsNs + "structMap",
                new XAttribute("ID", NewUuid()),
                new XAttribute("TYPE", SipPackConstants.StructMapTypePhysical),
                new XAttribute("LABEL", SipPackConstants.LabelCsip),
                new XElement(MetsNs + "div",
                    new XAttribute("ID", NewUuid()),
                    new XAttribute("TYPE", SipPackConstants.DivTypeOriginal),
                    new XAttribute("LABEL", SipPackConstants.Rep1Dir),
                    metadataLink,
                    new XElement(MetsNs + "div",
                        new XAttribute("ID", NewUuid()),
                        new XAttribute("LABEL", SipPackConstants.LabelData),
                        new XElement(MetsNs + "fptr",
                            new XAttribute("FILEID", input.DataFileGrpId))))));

            return new XDocument(new XDeclaration("1.0", "UTF-8", "yes"), mets);
        }

        public XElement BuildSimpleDc(IEnumerable<KeyValuePair<string, string>> truong)
        {
            var simpledc = new XElement(SipPackConstants.SimpleDcElement);

            foreach (var cap in truong)
            {
                simpledc.Add(new XElement(cap.Key, cap.Value));
            }

            return simpledc;
        }

        public XElement CreateMetsRoot(string objid, string label, string metsType, string schemaLocation)
        {
            return new XElement(MetsNs + "mets",
                new XAttribute(XNamespace.Xmlns + "xsi", XsiNs.NamespaceName),
                new XAttribute(XNamespace.Xmlns + "sip", SipNs.NamespaceName),
                new XAttribute(XNamespace.Xmlns + "csip", CsipNs.NamespaceName),
                new XAttribute(XNamespace.Xmlns + "xlink", XlinkNs.NamespaceName),
                new XAttribute("OBJID", objid),
                new XAttribute("LABEL", label),
                new XAttribute("TYPE", metsType),
                new XAttribute(CsipNs + "CONTENTINFORMATIONTYPE", SipPackConstants.ContentInformationType),
                new XAttribute("PROFILE", SipPackConstants.Profile),
                new XAttribute(XsiNs + "schemaLocation", schemaLocation));
        }

        public XElement BuildDmdSec(string dmdSecId, string mdRefId, string href, long size, string checksum,
            string created)
        {
            return new XElement(MetsNs + "dmdSec",
                new XAttribute("ID", dmdSecId),
                new XAttribute("CREATED", created),
                new XAttribute("STATUS", SipPackConstants.DmdStatusCurrent),
                new XElement(MetsNs + "mdRef",
                    new XAttribute("ID", mdRefId),
                    new XAttribute("LOCTYPE", SipPackConstants.LocType),
                    new XAttribute("MDTYPE", SipPackConstants.MdType),
                    new XAttribute("MDTYPEVERSION", SipPackConstants.MdTypeVersion),
                    new XAttribute(XlinkNs + "type", SipPackConstants.XlinkTypeSimple),
                    new XAttribute(XlinkNs + "href", href),
                    new XAttribute("MIMETYPE", SipPackConstants.MimeTypeXml),
                    new XAttribute("SIZE", size),
                    new XAttribute("CREATED", created),
                    new XAttribute("CHECKSUM", checksum),
                    new XAttribute("CHECKSUMTYPE", SipPackConstants.ChecksumType)));
        }

        public XElement BuildFileElement(PackFileDto file, string created, string href)
        {
            return new XElement(MetsNs + "file",
                new XAttribute("ID", file.FileId),
                new XAttribute("MIMETYPE", file.MimeType),
                new XAttribute("SIZE", file.Size),
                new XAttribute("CREATED", created),
                new XAttribute("CHECKSUM", file.Checksum),
                new XAttribute("CHECKSUMTYPE", SipPackConstants.ChecksumType),
                new XElement(MetsNs + "FLocat",
                    new XAttribute(XlinkNs + "type", SipPackConstants.XlinkTypeSimple),
                    new XAttribute(XlinkNs + "href", href),
                    new XAttribute("LOCTYPE", SipPackConstants.LocType)));
        }

        public byte[] Serialize(XDocument doc)
        {
            var settings = new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(false),
                Indent = true,
            };

            using var bo = new MemoryStream();
            using (var writer = XmlWriter.Create(bo, settings))
            {
                doc.Save(writer);
            }

            return bo.ToArray();
        }

        public string NewUuid() => SipPackConstants.UuidPrefix + Guid.NewGuid().ToString().ToUpperInvariant();

        public string NewFileId() => SipPackConstants.FileIdPrefix + Guid.NewGuid().ToString().ToUpperInvariant();

        public string FormatDateTime(DateTimeOffset value) => value.ToString(SipPackConstants.DateTimeFormat);

        public string GetMimeType(string extension)
        {
            return (extension ?? string.Empty).ToLowerInvariant() switch
            {
                ".pdf" => "application/pdf",
                ".txt" => "text/plain",
                ".rtf" => "application/rtf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".odt" => "application/vnd.oasis.opendocument.text",
                ".csv" => "text/csv",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".ods" => "application/vnd.oasis.opendocument.spreadsheet",
                ".htm" or ".html" => "text/html",
                ".ppt" => "application/vnd.ms-powerpoint",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".odp" => "application/vnd.oasis.opendocument.presentation",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".tif" or ".tiff" => "image/tiff",
                ".png" => "image/png",
                ".mp3" => "audio/mpeg",
                ".wma" => "audio/x-ms-wma",
                ".aac" => "audio/aac",
                ".avi" => "video/x-msvideo",
                ".wmv" => "video/x-ms-wmv",
                ".mov" or ".qt" => "video/quicktime",
                ".mpeg" or ".mpg" or ".mp4" => "video/mpeg",
                _ => SipPackConstants.MimeTypeOctetStream,
            };
        }
    }
}
