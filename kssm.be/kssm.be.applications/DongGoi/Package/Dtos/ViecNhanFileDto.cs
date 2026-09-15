using Microsoft.AspNetCore.Http;

namespace kssm.be.applications.DongGoi.Package.Dtos
{
    public class ViecNhanFileDto
    {
        public IFormFile File { get; set; } = default!;

        public string RelativePath { get; set; } = string.Empty;

        public string FolderName { get; set; } = string.Empty;

        public string Extension { get; set; } = string.Empty;

        public int Sequence { get; set; }
    }
}
