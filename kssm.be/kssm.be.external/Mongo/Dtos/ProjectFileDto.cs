namespace kssm.be.external.Mongo.Dtos
{
    public class ProjectFileDto
    {
        public string Id { get; set; } = string.Empty;

        public string FileName { get; set; } = string.Empty;

        public string ObjectKey { get; set; } = string.Empty;

        public int? PageCount { get; set; }
    }
}
