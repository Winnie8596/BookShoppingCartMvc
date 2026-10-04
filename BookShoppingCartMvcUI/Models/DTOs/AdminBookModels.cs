namespace BookShoppingCartMvcUI.Models.DTOs;

// filters for the admin book list, bad values fall back to the defaults
public class AdminBookQuery
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
    public static readonly IReadOnlyList<string> Sorts = ["newest", "title", "price", "price_desc", "stock"];

    // title or author
    public string? Search { get; set; }
    public int? GenreId { get; set; }
    public string? Sort { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = DefaultPageSize;
}

// one page of books + the filters used (so the page can keep them)
public class AdminBookListModel : PagedResult<AdminBookRowModel>
{
    public AdminBookListModel() : base(AdminBookQuery.DefaultPageSize) { }

    public string? Search { get; set; }
    public int? GenreId { get; set; }
    public string Sort { get; set; } = "newest";
    public List<Genre> Genres { get; set; } = new();
}

// just the columns the list needs
public class AdminBookRowModel
{
    public int Id { get; set; }
    public string? BookName { get; set; }
    public string? AuthorName { get; set; }
    public string? GenreName { get; set; }
    public decimal Price { get; set; }
    public string? Image { get; set; }
    // 0 if there's no Stock row
    public int Quantity { get; set; }
    // can't delete a book that has been ordered
    public bool HasOrders { get; set; }
}

// genre + how many books use it (can't delete it if it has books)
public record AdminGenreRowModel(int Id, string GenreName, int BookCount);
