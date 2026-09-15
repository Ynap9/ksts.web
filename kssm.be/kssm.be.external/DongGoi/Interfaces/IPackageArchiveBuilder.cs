using kssm.be.external.DongGoi.Dtos;

namespace kssm.be.external.DongGoi.Interfaces
{
    public interface IPackageArchiveBuilder
    {
        byte[] Build(string thuMucGoc, IEnumerable<PackEntryDto> entries);

        string ComputeSha256(byte[] content);
    }
}
