namespace kssm.be.external.S3.Dtos
{
    public class S3ConnectionDto
    {
        public string Url { get; set; } = string.Empty;

        public string Region { get; set; } = string.Empty;

        public string Bucket { get; set; } = string.Empty;

        public string AccessKey { get; set; } = string.Empty;

        public string SecretKey { get; set; } = string.Empty;

        public bool WithSSL { get; set; } = true;
    }
}
