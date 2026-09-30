namespace BookShoppingCartMvcUI
{
    public interface IHomeRepository
    {
        Task<(IEnumerable<Book> Books, int TotalCount)> GetBooks(
            string sTerm = "",
            int genreId = 0,
            double? minPrice = null,
            double? maxPrice = null,
            bool inStockOnly = false,
            string sortBy = "relevance",
            int pageNumber = 1,
            int pageSize = 12,
            int? minRating = null);
        Task<IEnumerable<Genre>> Genres();
        Task<Book?> GetBookDetails(int bookId);
        Task<IEnumerable<Book>> GetRelatedBooks(Book book, int count = 4);
        Task<List<AssistantBook>> FindBooksForAssistant(AssistantBookQuery query, int limit = 10);
    }
}
