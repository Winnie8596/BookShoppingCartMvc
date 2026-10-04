namespace BookShoppingCartMvcUI.Models.DTOs;

// cart line with the current price/stock from the db
public record CartLineModel(int BookId, string? BookName, string? Image, string? GenreName,
    decimal UnitPrice, int Quantity, int AvailableStock)
{
    public decimal Subtotal => UnitPrice * Quantity;
    public bool ExceedsStock => Quantity > AvailableStock;
    public bool CanIncrease => Quantity < AvailableStock;
}

public class CartViewModel
{
    public List<CartLineModel> Lines { get; init; } = new();
    public decimal Total => Lines.Sum(l => l.Subtotal);
    public bool IsEmpty => Lines.Count == 0;
    public bool HasStockProblems => Lines.Any(l => l.ExceedsStock);
    public bool CanCheckout => !IsEmpty && !HasStockProblems;
}

// result of a cart change. ErrorMessage is ok to show the user
public record CartUpdateResult(bool Succeeded, string? ErrorMessage);
