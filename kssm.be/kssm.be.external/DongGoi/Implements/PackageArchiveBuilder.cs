using kssm.be.external.DongGoi.Dtos;
using kssm.be.external.DongGoi.Interfaces;
using System.IO.Compression;
using System.Security.Cryptography;

namespace kssm.be.external.DongGoi.Implements
{
    public class PackageArchiveBuilder : IPackageArchiveBuilder
    {
        public byte[] Build(string thuMucGoc, IEnumerable<PackEntryDto> entries)
        {
            using var bo = new MemoryStream();

            using (var zip = new ZipArchive(bo, ZipArchiveMode.Create, true))
            {
                foreach (var entry in entries)
                {
                    var muc = zip.CreateEntry($"{thuMucGoc}/{entry.DuongDan}", CompressionLevel.Optimal);
                    using var luong = muc.Open();
                    luong.Write(entry.NoiDung, 0, entry.NoiDung.Length);
                }
            }

            return bo.ToArray();
        }

        public string ComputeSha256(byte[] content)
        {
            return Convert.ToHexString(SHA256.HashData(content));
        }
    }
}
