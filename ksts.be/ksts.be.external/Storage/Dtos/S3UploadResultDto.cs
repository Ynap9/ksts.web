namespace ksts.be.external.Storage.Dtos
{
    public class S3UploadResultDto
    {
        public string ObjectKey { get; set; } = string.Empty;

        public string Url { get; set; } = string.Empty;

        public string FileName { get; set; } = string.Empty;

        public long Length { get; set; }
    }
}
