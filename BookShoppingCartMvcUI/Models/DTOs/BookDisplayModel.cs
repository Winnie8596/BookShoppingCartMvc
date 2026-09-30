namespace BookShoppingCartMvcUI.Models.DTOs
{
    public class BookDisplayModel
    {
        public IEnumerable<Book> Books { get; set; }
        public IEnumerable<Genre> Genres { get; set; }
        public string STerm { get; set; } = "";
        public int GenreId { get; set; } = 0;
        public double? MinPrice { get; set; }
        public double? MaxPrice { get; set; }
        public bool InStockOnly { get; set; } = false;
        public int? MinRating { get; set; }
        public string SortBy { get; set; } = "relevance";
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 12;
        public int TotalCount { get; set; } = 0;
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
    }
}
