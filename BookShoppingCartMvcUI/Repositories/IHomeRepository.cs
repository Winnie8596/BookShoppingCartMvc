namespace BookShoppingCartMvcUI.Repositories;

public interface IHomeRepository
{
    Task<(IEnumerable<BookCardModel> Books, int TotalCount)> GetBooks(
        string sTerm = "",
        int genreId = 0,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        bool inStockOnly = false,
        string sortBy = "relevance",
        int pageNumber = 1,
        int pageSize = 12,
        int? minRating = null);
    Task<IEnumerable<Genre>> Genres();
    Task<BookCardModel?> GetBookDetails(int bookId);
    Task<IEnumerable<BookCardModel>> GetRelatedBooks(BookCardModel book, int count = 4);
    Task<List<AssistantBook>> FindBooksForAssistant(AssistantBookQuery query, int limit = 10);
}
