namespace BookShoppingCartMvcUI.Repositories
{
    // reviews. writes always use the logged in user's own review
    public interface IReviewRepository
    {
        Task<BookReviewsModel> GetBookReviews(int bookId);
        Task<RatingSummary> GetRatingSummary(int bookId);
        Task<ReviewInputModel?> GetMyReview(int bookId);
        // user's reviews, newest first
        Task<List<MyReviewModel>> GetMyReviews();
        Task<bool> HasPurchased(int bookId);
        Task<ReviewResult> Create(ReviewInputModel input);
        Task<ReviewResult> Update(ReviewInputModel input);
        Task<ReviewResult> Delete(int bookId);
    }
}
