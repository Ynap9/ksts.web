namespace kssm.be.external.DongGoi.Dtos
{
    public class PackDocDto
    {
        public PackFileDto Data { get; set; } = new();

        public PackFileDto Meta { get; set; } = new();

        public string DmdSecId { get; set; } = string.Empty;

        public string MdRefId { get; set; } = string.Empty;
    }
}
