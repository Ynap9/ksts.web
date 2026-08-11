namespace ksts.be.applications.Signing.Dtos
{
    public class CertDiagnosticDto
    {
        public int TotalCertificates { get; set; }

        public int SignableCertificates { get; set; }

        public List<string> StoreDiagnostics { get; set; } = new();
    }
}
