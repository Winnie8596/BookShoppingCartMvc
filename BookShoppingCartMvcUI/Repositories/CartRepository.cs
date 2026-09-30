using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BookShoppingCartMvcUI.Repositories
{
    public class CartRepository : ICartRepository
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<CartRepository> _logger;

        public CartRepository(ApplicationDbContext db, IHttpContextAccessor httpContextAccessor,
            UserManager<IdentityUser> userManager, ILogger<CartRepository>? logger = null)
        {
            _db = db;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger ?? NullLogger<CartRepository>.Instance;
        }
        public async Task<int> AddItem(int bookId, int qty)
        {
            var result = await TryAddItem(bookId, qty);
            return result.CartCount;
        }

        public async Task<CartAddResult> TryAddItem(int bookId, int qty)
        {
            string userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
                return await Failed(userId, "Please log in to add books to your cart.");
            if (qty < 1)
                return await Failed(userId, "Quantity must be at least 1.");

            // no transaction needed, each write is a single statement. the unique indexes on ShoppingCart(UserId)
            // and CartDetail(ShoppingCartId, BookId) stop a double click creating 2 carts/lines, the one that loses just retries
            for (int attempt = 1; ; attempt++)
            {
                try
                {
                    string? error = await AddOnce(userId, bookId, qty);
                    return error is null
                        ? new CartAddResult(true, null, await GetCartItemCount(userId))
                        : await Failed(userId, error);
                }
                catch (DbUpdateException ex) when (attempt < MaxAddAttempts)
                {
                    _logger.LogInformation(ex, "Adding book {BookId} for user {UserId} lost a race with a parallel request; retrying", bookId, userId);
                    _db.ChangeTracker.Clear();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Adding book {BookId} to the cart of user {UserId} failed", bookId, userId);
                    return await Failed(userId, "Something went wrong while adding the book to your cart.");
                }
            }
        }

        private const int MaxAddAttempts = 3;

        // returns null if it was added, otherwise the error message
        private async Task<string?> AddOnce(string userId, int bookId, int qty)
        {
            // always use the price/stock from the db, not the request
            var book = await _db.Books
                .Where(b => b.Id == bookId)
                .Select(b => new { b.Price, Available = b.Stock == null ? 0 : b.Stock.Quantity })
                .FirstOrDefaultAsync();
            if (book is null)
                return "This book no longer exists.";

            var cart = await GetCart(userId);
            if (cart is null)
            {
                // don't leave an empty cart behind if it fails
                if (qty > book.Available)
                    return StockMessage(book.Available, 0);
                cart = new ShoppingCart { UserId = userId };
                _db.ShoppingCarts.Add(cart);
                await _db.SaveChangesAsync();
            }

            // line already exists: check + increment in one UPDATE. reading then writing would let 2 parallel adds
            // both read 1 and both write 2. the whole line has to fit in stock, and it's written as a subtraction
            // so a huge qty can't overflow
            int maxBefore = book.Available - qty;
            int updated = await _db.CartDetails
                .Where(d => d.ShoppingCartId == cart.Id && d.BookId == bookId && d.Quantity <= maxBefore)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(d => d.Quantity, d => d.Quantity + qty)
                    .SetProperty(d => d.UnitPrice, book.Price));
            if (updated == 1)
                return null;

            int? alreadyInCart = await _db.CartDetails
                .Where(d => d.ShoppingCartId == cart.Id && d.BookId == bookId)
                .Select(d => (int?)d.Quantity)
                .FirstOrDefaultAsync();
            if (alreadyInCart is not null || qty > book.Available)
                return StockMessage(book.Available, alreadyInCart ?? 0);

            _db.CartDetails.Add(new CartDetail { BookId = bookId, ShoppingCartId = cart.Id, Quantity = qty, UnitPrice = book.Price });
            await _db.SaveChangesAsync();
            return null;
        }

        private async Task<CartAddResult> Failed(string userId, string message) =>
            new CartAddResult(false, message, await GetCartItemCount(userId));

        private static string StockMessage(int available, int alreadyInCart)
        {
            if (available <= 0)
                return "This book is out of stock.";
            if (alreadyInCart > 0)
                return $"Only {available} in stock, and you already have {alreadyInCart} in your cart.";
            return $"Only {available} in stock.";
        }


        // remove one copy (removes the line if it was the last). returns the new cart count
        public async Task<int> RemoveItem(int bookId)
        {
            string userId = GetUserId();
            if (!string.IsNullOrEmpty(userId))
            {
                // missing cart/line is fine, but real db errors aren't swallowed anymore
                var cartItem = await _db.CartDetails
                    .FirstOrDefaultAsync(d => d.ShoppingCart.UserId == userId && d.BookId == bookId);
                if (cartItem is not null)
                {
                    if (cartItem.Quantity <= 1)
                        _db.CartDetails.Remove(cartItem);
                    else
                        cartItem.Quantity--;
                    await _db.SaveChangesAsync();
                }
            }
            return await GetCartItemCount(userId);
        }

        public async Task<CartUpdateResult> UpdateQuantity(int bookId, int qty)
        {
            string userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
                return new CartUpdateResult(false, "Please log in to manage your cart.");
            if (qty < 1)
                return new CartUpdateResult(false, "Quantity must be at least 1.");

            var cartItem = await _db.CartDetails
                .Include(d => d.Book).ThenInclude(b => b.Stock)
                .FirstOrDefaultAsync(d => d.ShoppingCart.UserId == userId && d.BookId == bookId);
            if (cartItem is null)
                return new CartUpdateResult(false, "This book is not in your cart.");

            int available = cartItem.Book.Stock?.Quantity ?? 0;
            if (qty > available)
                return new CartUpdateResult(false, available <= 0
                    ? "This book is out of stock."
                    : $"Only {available} in stock.");

            cartItem.Quantity = qty;
            cartItem.UnitPrice = cartItem.Book.Price;
            await _db.SaveChangesAsync();
            return new CartUpdateResult(true, null);
        }

        // remove the whole line for a book. returns the new cart count
        public async Task<int> RemoveLine(int bookId)
        {
            string userId = GetUserId();
            if (!string.IsNullOrEmpty(userId))
            {
                await _db.CartDetails
                    .Where(d => d.ShoppingCart.UserId == userId && d.BookId == bookId)
                    .ExecuteDeleteAsync();
            }
            return await GetCartItemCount(userId);
        }

        public async Task ClearCart()
        {
            string userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
                return;
            await _db.CartDetails
                .Where(d => d.ShoppingCart.UserId == userId)
                .ExecuteDeleteAsync();
        }

        // user's cart with the current prices/stock from the db (not from when they added it)
        public async Task<CartViewModel> GetCartView()
        {
            var userId = GetUserId();
            var lines = await _db.CartDetails
                .Where(d => d.ShoppingCart.UserId == userId)
                .OrderBy(d => d.Id)
                .Select(d => new CartLineModel(
                    d.BookId,
                    d.Book.BookName,
                    d.Book.Image,
                    d.Book.Genre.GenreName,
                    d.Book.Price,
                    d.Quantity,
                    d.Book.Stock == null ? 0 : d.Book.Stock.Quantity))
                .ToListAsync();
            return new CartViewModel { Lines = lines };
        }

        public async Task<ShoppingCart> GetUserCart()
        {
            var userId = GetUserId();
            if (userId == null)
                throw new InvalidOperationException("Invalid userid");
            var shoppingCart = await _db.ShoppingCarts
                                  .Include(a => a.CartDetails)
                                  .ThenInclude(a => a.Book)
                                  .ThenInclude(a => a.Stock)
                                  .Include(a => a.CartDetails)
                                  .ThenInclude(a => a.Book)
                                  .ThenInclude(a => a.Genre)
                                  .Where(a => a.UserId == userId).FirstOrDefaultAsync();
            return shoppingCart;

        }
        public async Task<ShoppingCart> GetCart(string userId)
        {
            var cart = await _db.ShoppingCarts.FirstOrDefaultAsync(x => x.UserId == userId);
            return cart;
        }

        public async Task<int> GetCartItemCount(string userId = "")
        {
            if (string.IsNullOrEmpty(userId)) // updated line
            {
                userId = GetUserId();
            }
            // COUNT(*) in the db since this runs on every page
            return await _db.CartDetails.CountAsync(d => d.ShoppingCart.UserId == userId);
        }

        public async Task<bool> DoCheckout(CheckoutModel model)
        {
            var userId = GetUserId();
            // need a transaction here: order, order lines, stock and clearing the cart all go together,
            // if anything fails none of it should be saved
            await using var transaction = await _db.Database.BeginTransactionAsync();
            try
            {
                // logic
                // move data from cartDetail to order and order detail then we will remove cart detail
                if (string.IsNullOrEmpty(userId))
                    throw new UnauthorizedAccessException("User is not logged-in");
                var cart = await GetCart(userId);
                if (cart is null)
                    throw new InvalidOperationException("Invalid cart");
                // lock stock rows in book id order so 2 checkouts can't deadlock
                var cartDetail = await _db.CartDetails
                                    .Include(a => a.Book)
                                    .Where(a => a.ShoppingCartId == cart.Id)
                                    .OrderBy(a => a.BookId)
                                    .ToListAsync();
                if (cartDetail.Count == 0)
                    throw new InvalidOperationException("Cart is empty");
                var pendingRecord = await _db.orderStatuses.FirstOrDefaultAsync(s => s.StatusName == "Pending");
                if (pendingRecord is null)
                    throw new InvalidOperationException("Order status does not have Pending status");
                var order = new Order
                {
                    UserId = userId,
                    CreateDate = DateTime.UtcNow,
                    Name=model.Name,
                    Email=model.Email,
                    MobileNumber=model.MobileNumber,
                    PaymentMethod=model.PaymentMethod,
                    Address=model.Address,
                    IsPaid=false,
                    OrderStatusId = pendingRecord.Id
                };
                _db.Orders.Add(order);
                await _db.SaveChangesAsync();
                foreach(var item in cartDetail)
                {
                    // in case there are bad lines already saved in a cart
                    if (item.Quantity < 1)
                        throw new InvalidOperationException("Invalid quantity in cart");
                    var orderDetail = new OrderDetail
                    {
                        BookId = item.BookId,
                        OrderId = order.Id,
                        Quantity = item.Quantity,
                        // charge the current price, not the one from when it was added
                        UnitPrice = item.Book.Price
                    };
                    _db.OrderDetails.Add(orderDetail);

                    // check + decrement in one UPDATE. if we read then wrote, 2 checkouts could both see "1 left".
                    // this way the row lock makes the second one wait, then it finds not enough stock and updates nothing.
                    // missing stock row = no update either
                    int updated = await _db.Stocks
                        .Where(s => s.BookId == item.BookId && s.Quantity >= item.Quantity)
                        .ExecuteUpdateAsync(s => s.SetProperty(x => x.Quantity, x => x.Quantity - item.Quantity));
                    if (updated == 0)
                        throw new InvalidOperationException($"Not enough stock for book {item.BookId}");
                }

                // removing the cartdetails
                _db.CartDetails.RemoveRange(cartDetail);
                await _db.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                // rolls back on dispose, so no order is saved and no stock is taken
                _logger.LogWarning(ex, "Checkout failed for user {UserId}", userId);
                return false;
            }
        }

        private string GetUserId()
        {
            var principal = _httpContextAccessor.HttpContext.User;
            string userId = _userManager.GetUserId(principal);
            return userId;
        }


    }
}
