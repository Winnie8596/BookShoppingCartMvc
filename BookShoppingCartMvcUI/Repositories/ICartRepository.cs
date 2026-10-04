namespace BookShoppingCartMvcUI.Repositories;

// logged in user's cart, everything is scoped to them
public interface ICartRepository
{
    Task<CartAddResult> TryAddItem(int bookId, int qty);
    Task<int> RemoveItem(int bookId);
    Task<CartUpdateResult> UpdateQuantity(int bookId, int qty);
    Task<int> RemoveLine(int bookId);
    Task ClearCart();
    Task<CartViewModel> GetCartView();
    Task<int> GetCartItemCount();
    Task<ShoppingCart?> GetCart(string userId);
    Task<CheckoutResult> DoCheckout(CheckoutModel model);
}
