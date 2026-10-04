namespace BookShoppingCartMvcUI.Models.DTOs;

// a book as the catalog, details page and related books show it. used to be [NotMapped] fields on Book
public class BookCardModel
{
    public int Id { get; set; }
    public string BookName { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string? Image { get; set; }
    public decimal Price { get; set; }
    public int GenreId { get; set; }
    public string GenreName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    // null if there are no reviews
    public double? AverageRating { get; set; }
    public int ReviewCount { get; set; }
}
