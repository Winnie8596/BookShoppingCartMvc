using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BookShoppingCartMvcUI.Repositories
{
    public class WishlistRepository : IWishlistRepository
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ICartRepository _cartRepo;

        public WishlistRepository(ApplicationDbContext db, IHttpContextAccessor httpContextAccessor,
            UserManager<IdentityUser> userManager, ICartRepository cartRepo)
        {
            _db = db;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
            _cartRepo = cartRepo;
        }

        public async Task<WishlistResult> Add(int bookId)
        {
            string userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
                return new WishlistResult(false, "Please log in to use your wishlist.");
            if (!await _db.Books.AnyAsync(b => b.Id == bookId))
                return new WishlistResult(false, "This book no longer exists.");
            // already in the wishlist is fine, still just one row
            if (await IsInWishlist(userId, bookId))
                return new WishlistResult(true, null);

            _db.WishlistItems.Add(new WishlistItem { UserId = userId, BookId = bookId, CreatedAt = DateTime.UtcNow });
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // a parallel request might have added it first, unique index keeps one row
                if (!await IsInWishlist(userId, bookId))
                    throw;
            }
            return new WishlistResult(true, null);
        }

        public async Task Remove(int bookId)
        {
            string userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
                return;
            await _db.WishlistItems
                .Where(w => w.UserId == userId && w.BookId == bookId)
                .ExecuteDeleteAsync();
        }

        public async Task<bool> Contains(int bookId)
        {
            string userId = GetUserId();
            return !string.IsNullOrEmpty(userId) && await IsInWishlist(userId, bookId);
        }

        public async Task<List<WishlistLineModel>> GetWishlist()
        {
            string userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
                return new List<WishlistLineModel>();
            return await _db.WishlistItems
                .Where(w => w.UserId == userId)
                .OrderByDescending(w => w.CreatedAt).ThenByDescending(w => w.Id)
                .Select(w => new WishlistLineModel(
                    w.BookId,
                    w.Book.BookName,
                    w.Book.AuthorName,
                    w.Book.Image,
                    w.Book.Price,
                    w.Book.Stock == null ? 0 : w.Book.Stock.Quantity,
                    w.CreatedAt))
                .ToListAsync();
        }

        // add one to the cart (with the stock checks), then remove it from the wishlist
        public async Task<WishlistResult> MoveToCart(int bookId)
        {
            string userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
                return new WishlistResult(false, "Please log in to use your wishlist.");
            if (!await IsInWishlist(userId, bookId))
                return new WishlistResult(false, "This book is not in your wishlist.");

            var added = await _cartRepo.TryAddItem(bookId, 1);
            if (!added.Succeeded)
                return new WishlistResult(false, added.ErrorMessage);

            await Remove(bookId);
            return new WishlistResult(true, null);
        }

        private Task<bool> IsInWishlist(string userId, int bookId) =>
            _db.WishlistItems.AnyAsync(w => w.UserId == userId && w.BookId == bookId);

        private string GetUserId()
        {
            var principal = _httpContextAccessor.HttpContext.User;
            return _userManager.GetUserId(principal);
        }
    }
}
