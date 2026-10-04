namespace BookShoppingCartMvcUI.Models.DTOs;

public class BookDetailsModel
{
    public BookCardModel Book { get; set; } = null!;
    public IEnumerable<BookCardModel> RelatedBooks { get; set; } = Enumerable.Empty<BookCardModel>();
    public BookReviewsModel Reviews { get; set; } = new();
}
