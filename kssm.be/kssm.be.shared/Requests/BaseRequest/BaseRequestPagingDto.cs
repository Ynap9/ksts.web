using Microsoft.AspNetCore.Mvc;

namespace kssm.be.shared.Requests.BaseRequest
{
    public class BaseRequestPagingDto
    {
        [FromQuery(Name = "pageSize")]
        public int PageSize { get; set; }

        [FromQuery(Name = "pageNumber")]
        public int PageNumber { get; set; }

        private string? _keyword { get; set; }
        [FromQuery(Name = "keyword")]
        public string? Keyword
        {
            get => _keyword;
            set => _keyword = value?.Trim();
        }

        public int GetSkip()
        {
            int skip = (PageNumber - 1) * PageSize;
            if (skip < 0)
            {
                skip = 0;
            }
            return skip;
        }

        public List<string> Sort { get; set; } = new();
    }
}
