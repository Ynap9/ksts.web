namespace kssm.be.external.KySo.Certificates.Dtos
{
    public class CertScanResultDto
    {
        public List<SignCertDto> Certificates { get; set; } = new();

        public List<string> StoreDiagnostics { get; set; } = new();
    }
}
