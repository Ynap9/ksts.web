namespace kssm.be.shared.Requests.BaseRequest
{
    public static class PagingParameter
    {
        public const int DefaultPageSize = -1;
    }

    public static class PagingExtension
    {
        /// <summary>
        /// Hàm phân trang
        /// </summary>
        public static IQueryable<T> Paging<T>(this IQueryable<T> query, BaseRequestPagingDto input)
        {
            if (input.PageSize != PagingParameter.DefaultPageSize)
            {
                query = query.Skip(input.GetSkip()).Take(input.PageSize);
            }
            return query;
        }
    }
}
