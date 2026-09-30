namespace BookShoppingCartMvcUI.Models.DTOs
{
    public class BookDetailsModel
    {
        public Book Book { get; set; }
        public IEnumerable<Book> RelatedBooks { get; set; } = Enumerable.Empty<Book>();
        public BookReviewsModel Reviews { get; set; } = new();
    }
}
