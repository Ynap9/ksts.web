namespace kssm.be.external.Mongo.Dtos
{
    public class ProjectDto
    {
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string? Code { get; set; }

        public string? Status { get; set; }
    }
}
