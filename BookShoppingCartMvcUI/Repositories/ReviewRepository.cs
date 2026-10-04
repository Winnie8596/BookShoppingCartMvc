using BookShoppingCartMvcUI.Shared;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace BookShoppingCartMvcUI.Repositories;

public class ReviewRepository : IReviewRepository
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public ReviewRepository(ApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<BookReviewsModel> GetBookReviews(int bookId)
    {
        var userId = _currentUser.UserId;
        var purchased = PurchasedLines();

        // newest first, verified = the reviewer actually bought it
        var rows = await (from r in _db.Reviews
                          join u in _db.Users on r.UserId equals u.Id
                          where r.BookId == bookId
                          orderby r.CreatedAt descending, r.Id descending
                          select new
                          {
                              r.UserId,
                              r.Rating,
                              r.Title,
                              r.Comment,
                              u.UserName,
                              r.CreatedAt,
                              r.UpdatedAt,
                              IsVerified = purchased.Any(od => od.BookId == r.BookId && od.Order.UserId == r.UserId)
                          })
                          .ToListAsync();

        bool signedIn = !string.IsNullOrEmpty(userId);
        return new BookReviewsModel
        {
            Summary = await GetRatingSummary(bookId),
            Reviews = rows.Select(r => new ReviewDisplayModel(
                r.Rating, r.Title, r.Comment, ReviewerName(r.UserName), r.CreatedAt, r.UpdatedAt,
                r.IsVerified, signedIn && r.UserId == userId)).ToList(),
            IsSignedIn = signedIn,
            HasPurchased = signedIn && await HasPurchased(userId!, bookId),
            HasReviewed = signedIn && rows.Any(r => r.UserId == userId)
        };
    }

    // db groups the counts per star, avg is worked out from those
    public async Task<RatingSummary> GetRatingSummary(int bookId)
    {
        var counts = await _db.Reviews
            .Where(r => r.BookId == bookId)
            .GroupBy(r => r.Rating)
            .Select(g => new { Rating = g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Rating, g => g.Count);
        return new RatingSummary(counts);
    }

    public async Task<ReviewInputModel?> GetMyReview(int bookId)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
            return null;
        return await _db.Reviews
            .Where(r => r.UserId == userId && r.BookId == bookId)
            .Select(r => new ReviewInputModel { BookId = r.BookId, Rating = r.Rating, Title = r.Title, Comment = r.Comment })
            .FirstOrDefaultAsync();
    }

    public async Task<List<MyReviewModel>> GetMyReviews()
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
            return new List<MyReviewModel>();
        return await _db.Reviews
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt).ThenByDescending(r => r.Id)
            .Select(r => new MyReviewModel(r.BookId, r.Book.BookName, r.Book.Image, r.Rating, r.Title, r.Comment,
                r.CreatedAt, r.UpdatedAt))
            .ToListAsync();
    }

    public async Task<int> CountMyReviews()
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
            return 0;
        return await _db.Reviews.CountAsync(r => r.UserId == userId);
    }

    public async Task<bool> HasPurchased(int bookId)
    {
        var userId = _currentUser.UserId;
        return !string.IsNullOrEmpty(userId) && await HasPurchased(userId, bookId);
    }

    public async Task<ReviewResult> Create(ReviewInputModel input)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
            return new ReviewResult(false, "Please log in to review books.");
        input = Normalize(input);
        if (Validate(input) is string error)
            return new ReviewResult(false, error);
        if (await HasReviewed(userId, input.BookId))
            return AlreadyReviewed;
        // based on their own orders only, not anything in the request
        if (!await HasPurchased(userId, input.BookId))
            return new ReviewResult(false, "Only customers who bought this book can review it.");

        _db.Reviews.Add(new Review
        {
            UserId = userId,
            BookId = input.BookId,
            Rating = input.Rating!.Value,
            Title = input.Title,
            Comment = input.Comment!,
            CreatedAt = DateTime.UtcNow
        });
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // a parallel request might have saved one first, unique index keeps it to one row
            if (await HasReviewed(userId, input.BookId))
                return AlreadyReviewed;
            throw;
        }
        return new ReviewResult(true, null);
    }

    public async Task<ReviewResult> Update(ReviewInputModel input)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
            return new ReviewResult(false, "Please log in to review books.");
        input = Normalize(input);
        if (Validate(input) is string error)
            return new ReviewResult(false, error);

        // looked up by the logged in user so they can only change their own
        var review = await _db.Reviews.FirstOrDefaultAsync(r => r.UserId == userId && r.BookId == input.BookId);
        if (review is null)
            return NotReviewed;

        review.Rating = input.Rating!.Value;
        review.Title = input.Title;
        review.Comment = input.Comment!;
        review.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return new ReviewResult(true, null);
    }

    public async Task<ReviewResult> Delete(int bookId)
    {
        var userId = _currentUser.UserId;
        if (string.IsNullOrEmpty(userId))
            return new ReviewResult(false, "Please log in to review books.");
        int deleted = await _db.Reviews
            .Where(r => r.UserId == userId && r.BookId == bookId)
            .ExecuteDeleteAsync();
        return deleted == 0 ? NotReviewed : new ReviewResult(true, null);
    }

    private static readonly ReviewResult AlreadyReviewed =
        new(false, "You have already reviewed this book. You can edit your review instead.");
    private static readonly ReviewResult NotReviewed = new(false, "You have not reviewed this book.");

    // order lines that count as bought: not cancelled/returned/refunded (deleted orders are filtered out already)
    private IQueryable<OrderDetail> PurchasedLines() =>
        _db.OrderDetails.Where(od => !OrderWorkflow.NotSold.Contains(od.Order.OrderStatus.StatusName));

    private Task<bool> HasPurchased(string userId, int bookId) =>
        PurchasedLines().AnyAsync(od => od.Order.UserId == userId && od.BookId == bookId);

    private Task<bool> HasReviewed(string userId, int bookId) =>
        _db.Reviews.AnyAsync(r => r.UserId == userId && r.BookId == bookId);

    private static ReviewInputModel Normalize(ReviewInputModel input) => new()
    {
        BookId = input.BookId,
        Rating = input.Rating,
        Title = string.IsNullOrWhiteSpace(input.Title) ? null : input.Title.Trim(),
        Comment = input.Comment?.Trim()
    };

    // same rules as the form, checked again here in case something skips the form
    private static string? Validate(ReviewInputModel input)
    {
        var results = new List<ValidationResult>();
        return Validator.TryValidateObject(input, new ValidationContext(input), results, validateAllProperties: true)
            ? null
            : results[0].ErrorMessage;
    }

    // reviews are public so don't show the email, just the start of the part before the @
    public static string ReviewerName(string? userName)
    {
        var local = (userName ?? "").Split('@')[0].Trim();
        if (local.Length == 0)
            return "Customer";
        return local.Length <= 3 ? local[0] + "***" : local[..3] + "***";
    }
}
