namespace kssm.be.shared.Requests.BaseRequest
{
    public class BaseResponsePagingDto<T>
    {
        public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
        public int TotalItems { get; set; }
        public object? CustomData { get; set; }
    }
}
