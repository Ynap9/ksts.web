namespace kssm.be.shared.Settings
{
    public class SaoMaiSettings
    {
        public string MongoConnectionString { get; set; } = string.Empty;

        public string CallbackUrl { get; set; } = string.Empty;

        public string CallbackApiKey { get; set; } = string.Empty;

        public int CallbackTimeoutSeconds { get; set; } = 30;
    }
}
