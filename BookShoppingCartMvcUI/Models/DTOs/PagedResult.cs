namespace BookShoppingCartMvcUI.Models.DTOs;

// one page of rows + the paging numbers, the list pages add their own filters on top
public abstract class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;

    protected PagedResult(int defaultPageSize)
    {
        PageSize = defaultPageSize;
    }
}
