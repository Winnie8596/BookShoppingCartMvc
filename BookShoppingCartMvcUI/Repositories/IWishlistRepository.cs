namespace BookShoppingCartMvcUI.Repositories
{
    // logged in user's wishlist, everything is scoped to them
    public interface IWishlistRepository
    {
        Task<WishlistResult> Add(int bookId);
        Task Remove(int bookId);
        Task<bool> Contains(int bookId);
        Task<List<WishlistLineModel>> GetWishlist();
        Task<WishlistResult> MoveToCart(int bookId);
    }
}
