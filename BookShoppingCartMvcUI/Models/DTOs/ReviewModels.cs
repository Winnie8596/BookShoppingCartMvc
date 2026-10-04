using System.ComponentModel.DataAnnotations;

namespace BookShoppingCartMvcUI.Models.DTOs;

// posted when writing/editing a review. user always comes from the login, not the form
public class ReviewInputModel
{
    [Range(1, int.MaxValue)]
    public int BookId { get; set; }

    [Required(ErrorMessage = "Please choose a rating.")]
    [Range(Review.MinRating, Review.MaxRating, ErrorMessage = "Rating must be between 1 and 5 stars.")]
    public int? Rating { get; set; }

    [MaxLength(Review.TitleMaxLength, ErrorMessage = "The title can be at most 100 characters.")]
    public string? Title { get; set; }

    [Required(ErrorMessage = "Please write a comment.")]
    [StringLength(Review.CommentMaxLength, MinimumLength = Review.CommentMinLength,
        ErrorMessage = "The comment must be between 10 and 1000 characters.")]
    public string? Comment { get; set; }
}

// review on the book page. verified purchase is worked out from their orders
public record ReviewDisplayModel(int Rating, string? Title, string Comment, string ReviewerName,
    DateTime CreatedAt, DateTime? UpdatedAt, bool IsVerifiedPurchase, bool IsMine);

// avg, count and how many per star, from the Review rows
public class RatingSummary
{
    // reviews per star (1-5), index 0 = 1 star
    public IReadOnlyList<int> Distribution { get; }
    public int ReviewCount { get; }
    // null if no reviews
    public double? AverageRating { get; }

    public RatingSummary(IReadOnlyDictionary<int, int> countsByRating)
    {
        Distribution = Enumerable.Range(Review.MinRating, Review.MaxRating - Review.MinRating + 1)
            .Select(stars => countsByRating.TryGetValue(stars, out var count) ? count : 0)
            .ToList();
        ReviewCount = Distribution.Sum();
        AverageRating = ReviewCount == 0
            ? null
            : Distribution.Select((count, i) => (double)count * (i + 1)).Sum() / ReviewCount;
    }

    public static RatingSummary Empty { get; } = new(new Dictionary<int, int>());

    public int CountFor(int stars) => Distribution[stars - 1];
}

// all the review stuff the book page needs for the current user
public class BookReviewsModel
{
    public RatingSummary Summary { get; set; } = RatingSummary.Empty;
    public List<ReviewDisplayModel> Reviews { get; set; } = new();
    public bool IsSignedIn { get; set; }
    public bool HasPurchased { get; set; }
    public bool HasReviewed { get; set; }
    public bool CanWriteReview => IsSignedIn && HasPurchased && !HasReviewed;
}

public record ReviewResult(bool Succeeded, string? ErrorMessage);
