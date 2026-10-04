namespace BookShoppingCartMvcUI.Models.DTOs;

// filters for the inventory page, bad values fall back to the defaults
public class InventoryQuery
{
    public const int DefaultPageSize = 25;
    public const int MaxPageSize = 100;
    // "low" = 1 up to the threshold, out of stock has its own filter
    public static readonly IReadOnlyList<string> Statuses = ["all", "out", "low", "in"];
    public static readonly IReadOnlyList<string> Sorts = ["stock", "stock_desc", "title"];

    // title or author
    public string? Search { get; set; }
    public string? Status { get; set; }
    public string? Sort { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = DefaultPageSize;
}

// one page of inventory + counts per status + filters
public class InventoryListModel : PagedResult<StockDisplayModel>
{
    public InventoryListModel() : base(InventoryQuery.DefaultPageSize) { }

    public string? Search { get; set; }
    public string Status { get; set; } = "all";
    public string Sort { get; set; } = "stock";

    // only uses the search (not the status filter) so the tiles still make sense
    public int InStockCount { get; set; }
    public int LowStockCount { get; set; }
    public int OutOfStockCount { get; set; }
}
