namespace ksts.be.shared.Settings
{
    public class FileSettings
    {
        public string Path { get; set; } = string.Empty;

        public long LimitUpload { get; set; }

        public string AllowExtension { get; set; } = string.Empty;
    }
}
