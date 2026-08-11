using Microsoft.AspNetCore.Http;

namespace ksts.be.applications.Template.Dtos
{
    public class SuggestPlacementDto
    {
        public IFormFile? FilePdf { get; set; }

        public IFormFile? AnhDauDo { get; set; }

        public IFormFile? AnhChuKyTuoi { get; set; }

        public int PageNumber { get; set; } = 1;
    }
}
