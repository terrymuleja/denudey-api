namespace Denudey.Api.Models
{
    public class PagedResult<T>
    {
        public int TotalItems { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public List<T> Items { get; set; } = [];
        public bool HasNextPage { get; set; }
    }
}
