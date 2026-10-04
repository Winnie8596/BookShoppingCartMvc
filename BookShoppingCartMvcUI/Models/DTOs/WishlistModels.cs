namespace BookShoppingCartMvcUI.Models.DTOs;

// wishlist row, price/stock from the db
public record WishlistLineModel(int BookId, string? BookName, string? AuthorName, string? Image,
    decimal Price, int AvailableStock, DateTime CreatedAt)
{
    public bool InStock => AvailableStock > 0;
}

public record WishlistResult(bool Succeeded, string? ErrorMessage);
