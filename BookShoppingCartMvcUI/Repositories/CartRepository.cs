using BookShoppingCartMvcUI.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BookShoppingCartMvcUI.Repositories;

public class CartRepository : ICartRepository
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<CartRepository> _logger;

    public CartRepository(ApplicationDbContext db, ICurrentUser currentUser, ILogger<CartRepository>? logger = null)
    {
        _db = db;
        _currentUser = currentUser;
        _logger = logger ?? NullLogger<CartRepository>.Instance;
    }

    public async Task<CartAddResult> TryAddItem(int bookId, int qty)
    {
        var userId = _currentUser.UserId;
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
                    ? new CartAddResult(true, null, await CountFor(userId))
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

    private async Task<CartAddResult> Failed(string? userId, string message) =>
        new CartAddResult(false, message, await CountFor(userId));

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
        var userId = _currentUser.UserId;
        if (!string.IsNullOrEmpty(userId))
        {
            // two conditional statements instead of read-change-save, so two quick clicks can't both read 2
            // and both write 1. missing cart/line is fine, real db errors aren't swallowed
            var line = _db.CartDetails.Where(d => d.ShoppingCart.UserId == userId && d.BookId == bookId);
            int decremented = await line
                .Where(d => d.Quantity > 1)
                .ExecuteUpdateAsync(s => s.SetProperty(d => d.Quantity, d => d.Quantity - 1));
            if (decremented == 0)
                await line.Where(d => d.Quantity <= 1).ExecuteDeleteAsync();
        }
        return await CountFor(userId);
    }

    public async Task<CartUpdateResult> UpdateQuantity(int bookId, int qty)
    {
        var userId = _currentUser.UserId;
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
        var userId = _currentUser.UserId;
        if (!string.IsNullOrEmpty(userId))
        {
            await _db.CartDetails
                .Where(d => d.ShoppingCart.UserId == userId && d.BookId == bookId)
                .ExecuteDeleteAsync();
        }
        return await CountFor(userId);
    }

    public async Task ClearCart()
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
            return;
        await _db.CartDetails
            .Where(d => d.ShoppingCart.UserId == userId)
            .ExecuteDeleteAsync();
    }

    // user's cart with the current prices/stock from the db (not from when they added it)
    public async Task<CartViewModel> GetCartView()
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
            return new CartViewModel();
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

    public Task<ShoppingCart?> GetCart(string userId) =>
        _db.ShoppingCarts.FirstOrDefaultAsync(x => x.UserId == userId);

    public Task<int> GetCartItemCount() => CountFor(_currentUser.UserId);

    // COUNT(*) in the db since this runs on every page
    private async Task<int> CountFor(string? userId) =>
        string.IsNullOrEmpty(userId) ? 0 : await _db.CartDetails.CountAsync(d => d.ShoppingCart.UserId == userId);

    public async Task<CheckoutResult> DoCheckout(CheckoutModel model)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
            return CheckoutResult.Failed(CheckoutFailure.NotLoggedIn, "Please log in to place an order.");

        // need a transaction here: order, order lines, stock and clearing the cart all go together,
        // if anything fails none of it should be saved. returning without Commit rolls it back on dispose
        await using var transaction = await _db.Database.BeginTransactionAsync();
        try
        {
            var result = await CheckoutCart(userId, model);
            if (result.Succeeded)
            {
                await transaction.CommitAsync();
            }
            else
            {
                _logger.LogWarning("Checkout failed for user {UserId}: {Failure} {Message}", userId, result.Failure, result.ErrorMessage);
                _db.ChangeTracker.Clear();
            }
            return result;
        }
        catch (Exception ex)
        {
            // rolls back on dispose, so no order is saved and no stock is taken
            _logger.LogWarning(ex, "Checkout failed for user {UserId}", userId);
            _db.ChangeTracker.Clear();
            return CheckoutResult.Failed(CheckoutFailure.Error, "Something went wrong while placing your order. Nothing was charged, please try again.");
        }
    }

    // move the cart lines into an order + order lines, take the stock, then empty the cart
    private async Task<CheckoutResult> CheckoutCart(string userId, CheckoutModel model)
    {
        // lock stock rows in book id order so 2 checkouts can't deadlock
        var cartDetail = await _db.CartDetails
                            .Include(a => a.Book)
                            .Where(a => a.ShoppingCart.UserId == userId)
                            .OrderBy(a => a.BookId)
                            .ToListAsync();
        if (cartDetail.Count == 0)
            return CheckoutResult.Failed(CheckoutFailure.EmptyCart, "Your cart is empty.");
        // in case there are bad lines already saved in a cart
        if (cartDetail.Any(d => d.Quantity < 1))
            return CheckoutResult.Failed(CheckoutFailure.InvalidQuantity, "Your cart has a line with an invalid quantity. Please update your cart.");

        var pendingId = await _db.OrderStatuses
            .Where(s => s.StatusName == OrderWorkflow.Pending)
            .Select(s => (int?)s.Id)
            .FirstOrDefaultAsync();
        if (pendingId is null)
            return CheckoutResult.Failed(CheckoutFailure.MissingPendingStatus, "Orders can't be placed right now. Please try again later.");

        var order = new Order
        {
            UserId = userId,
            CreateDate = DateTime.UtcNow,
            // the checkout form is validated before we get here, so these are set
            Name = model.Name!,
            Email = model.Email!,
            MobileNumber = model.MobileNumber!,
            PaymentMethod = model.PaymentMethod!,
            Address = model.Address!,
            IsPaid = false,
            OrderStatusId = pendingId.Value
        };
        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        foreach (var item in cartDetail)
        {
            // check + decrement in one UPDATE. if we read then wrote, 2 checkouts could both see "1 left".
            // this way the row lock makes the second one wait, then it finds not enough stock and updates nothing.
            // missing stock row = no update either
            int updated = await _db.Stocks
                .Where(s => s.BookId == item.BookId && s.Quantity >= item.Quantity)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.Quantity, x => x.Quantity - item.Quantity));
            if (updated == 0)
                return CheckoutResult.Failed(CheckoutFailure.OutOfStock,
                    $"There isn't enough stock left for \"{item.Book.BookName}\". Please update your cart.");

            _db.OrderDetails.Add(new OrderDetail
            {
                BookId = item.BookId,
                OrderId = order.Id,
                Quantity = item.Quantity,
                // charge the current price, not the one from when it was added
                UnitPrice = item.Book.Price
            });
        }

        _db.CartDetails.RemoveRange(cartDetail);
        await _db.SaveChangesAsync();
        return CheckoutResult.Success(order.Id);
    }
}
