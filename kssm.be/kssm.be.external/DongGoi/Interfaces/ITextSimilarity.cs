namespace kssm.be.external.DongGoi.Interfaces
{
    public interface ITextSimilarity
    {
        string Normalize(string? text);

        double Score(string? left, string? right);
    }
}
