namespace BookShoppingCartMvcUI.Models.DTOs
{
    // result of adding to cart. ErrorMessage is ok to show the user
    public record CartAddResult(bool Succeeded, string? ErrorMessage, int CartCount);
}
