namespace kssm.be.shared.Constants.DongGoi
{
    public static class SipPackConstants
    {
        public const string NsMets = "http://www.loc.gov/METS/";
        public const string NsXsi = "http://www.w3.org/2001/XMLSchema-instance";
        public const string NsXlink = "http://www.w3.org/1999/xlink";
        public const string NsCsip = "https://DILCIS.eu/XML/METS/CSIPExtensionMETS";
        public const string NsSip = "https://DILCIS.eu/XML/METS/SIPExtensionMETS";

        public const string SchemaLocationRoot =
            "http://www.loc.gov/METS/ schemas/mets1_12.xsd " +
            "http://www.w3.org/1999/xlink schemas/xlink.xsd " +
            "https://dilcis.eu/XML/METS/CSIPExtensionMETS schemas/DILCISExtensionMETS.xsd " +
            "https://dilcis.eu/XML/METS/SIPExtensionMETS schemas/DILCISExtensionSIPMETS.xsd";

        public const string SchemaLocationRep =
            "http://www.loc.gov/METS/ ../../schemas/mets1_12.xsd " +
            "http://www.w3.org/1999/xlink ../../schemas/xlink.xsd " +
            "https://dilcis.eu/XML/METS/CSIPExtensionMETS ../../schemas/DILCISExtensionMETS.xsd " +
            "https://dilcis.eu/XML/METS/SIPExtensionMETS ../../schemas/DILCISExtensionSIPMETS.xsd";

        public const string Profile = "https://earkcsip.dilcis.eu/profile/E-ARK-CSIP.xml";
        public const string MetsTypeHoSo = "Mixed";
        public const string MetsTypeTaiLieu = "Collection";
        public const string ContentInformationType = "MIXED";
        public const string OaisPackageType = "SIP";
        public const string RecordStatusDefault = "NEW";
        public const string RepObjid = "rep1";

        public const string AgentRole = "CREATOR";
        public const string AgentTypeSoftware = "OTHER";
        public const string AgentOtherTypeSoftware = "SOFTWARE";
        public const string AgentTypeIndividual = "INDIVIDUAL";
        public const string AgentName = "kssm.be";
        public const string NoteTypeIdentificationCode = "IDENTIFICATIONCODE";
        public const string AltRecordIdType = "SUBMISSIONAGREEMENT";

        public const string MdType = "DC";
        public const string MdTypeVersion = "1.0.0";
        public const string LocType = "URL";
        public const string XlinkTypeSimple = "simple";
        public const string DmdStatusCurrent = "CURRENT";

        public const string ChecksumType = "SHA-256";
        public const string MimeTypeXml = "application/xml";
        public const string MimeTypeOctetStream = "application/octet-stream";
        public const string MimeTypeZip = "application/zip";

        public const string UuidPrefix = "uuid-";
        public const string FileIdPrefix = "ID-";

        public const string MetsFileName = "METS.xml";
        public const string EadFileName = "EAD.xml";
        public const string MetadataDir = "metadata";
        public const string DescriptiveDir = "descriptive";
        public const string RepresentationsDir = "representations";
        public const string Rep1Dir = "rep1";
        public const string SchemasDir = "schemas";
        public const string DataDir = "data";

        public const string UseSchemas = "Schemas";
        public const string UseRepresentations = "Representations/rep1";
        public const string UseData = "Data";
        public const string LabelCsip = "CSIP";
        public const string LabelMetadata = "Metadata";
        public const string LabelData = "Data";
        public const string LabelMetadataLink = "MetadataLink";
        public const string LabelMetadataLinkFile = "MetadataLink/File";
        public const string StructMapTypePhysical = "PHYSICAL";
        public const string DivTypeOriginal = "ORIGINAL";

        public const string EadDocPrefix = "EAD_DOC_";
        public const string EadPicPrefix = "EAD_PIC_";
        public const string EadMediaPrefix = "EAD_MEDIA_";

        public const string SimpleDcElement = "simpledc";
        public const string DateTimeFormat = "yyyy-MM-ddTHH:mm:ss.fffzzz";
        public const string ZipExtension = ".zip";

        public const string SchemaTemplateDir = "Templates";
        public const string SchemaTemplateSubDir = "schemas";
    }
}
