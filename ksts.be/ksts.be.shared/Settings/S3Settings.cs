using Microsoft.Extensions.Configuration;

namespace ksts.be.shared.Settings
{
    public class S3Settings
    {
        [ConfigurationKeyName("S3_URL")]
        public string Url { get; set; } = string.Empty;

        [ConfigurationKeyName("S3_REGION")]
        public string Region { get; set; } = string.Empty;

        [ConfigurationKeyName("S3_BUCKET")]
        public string Bucket { get; set; } = string.Empty;

        [ConfigurationKeyName("S3_ACCESS_KEY")]
        public string AccessKey { get; set; } = string.Empty;

        [ConfigurationKeyName("S3_SECRET_KEY")]
        public string SecretKey { get; set; } = string.Empty;

        [ConfigurationKeyName("S3_WITH_SSL")]
        public bool WithSSL { get; set; } = true;
    }
}
