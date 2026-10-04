namespace BookShoppingCartMvcUI.Models.DTOs;

public class BookDisplayModel : PagedResult<BookCardModel>
{
    public BookDisplayModel() : base(12) { }

    public IEnumerable<Genre> Genres { get; set; } = Enumerable.Empty<Genre>();
    public string STerm { get; set; } = "";
    public int GenreId { get; set; } = 0;
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public bool InStockOnly { get; set; } = false;
    public int? MinRating { get; set; }
    public string SortBy { get; set; } = "relevance";
}
