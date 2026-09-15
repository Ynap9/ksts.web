namespace kssm.be.external.DongGoi.Dtos
{
    public class RootMetsInputDto
    {
        public string Objid { get; set; } = string.Empty;

        public string Label { get; set; } = string.Empty;

        public string MetsType { get; set; } = string.Empty;

        public string FileCode { get; set; } = string.Empty;

        public string? MaPhong { get; set; }

        public string Created { get; set; } = string.Empty;

        public string EadDmdSecId { get; set; } = string.Empty;

        public string EadMdRefId { get; set; } = string.Empty;

        public long EadSize { get; set; }

        public string EadChecksum { get; set; } = string.Empty;

        public string SchemasFileGrpId { get; set; } = string.Empty;

        public List<PackFileDto> Schemas { get; set; } = new();

        public string RepFileGrpId { get; set; } = string.Empty;

        public PackFileDto RepMets { get; set; } = new();
    }
}
