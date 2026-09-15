using kssm.be.external.DongGoi.Dtos;

namespace kssm.be.external.DongGoi.Interfaces
{
    public interface IPdfFormatInspector
    {
        PdfInspectResultDto Inspect(byte[] bytes);
    }
}
