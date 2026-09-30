namespace BookShoppingCartMvcUI.Repositories
{
    public interface ICartRepository
    {
        Task<int> AddItem(int bookId, int qty);
        Task<CartAddResult> TryAddItem(int bookId, int qty);
        Task<int> RemoveItem(int bookId);
        Task<CartUpdateResult> UpdateQuantity(int bookId, int qty);
        Task<int> RemoveLine(int bookId);
        Task ClearCart();
        Task<CartViewModel> GetCartView();
        Task<ShoppingCart> GetUserCart();
        Task<int> GetCartItemCount(string userId = "");
        Task<ShoppingCart> GetCart(string userId);
        Task<bool> DoCheckout(CheckoutModel model);
    }
}
