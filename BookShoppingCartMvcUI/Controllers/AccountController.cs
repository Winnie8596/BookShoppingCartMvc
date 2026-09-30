using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookShoppingCartMvcUI.Controllers
{
    // the customer's "My Account" pages
    [Authorize]
    public class AccountController : Controller
    {
        private readonly IReviewRepository _reviewRepo;
        private readonly IUserOrderRepository _orderRepo;
        private readonly IWishlistRepository _wishlistRepo;

        public AccountController(IReviewRepository reviewRepo, IUserOrderRepository orderRepo, IWishlistRepository wishlistRepo)
        {
            _reviewRepo = reviewRepo;
            _orderRepo = orderRepo;
            _wishlistRepo = wishlistRepo;
        }

        public async Task<IActionResult> Index()
        {
            // these only return the logged in user's stuff
            var orders = await _orderRepo.GetMyOrders();
            var wishlist = await _wishlistRepo.GetWishlist();
            var reviews = await _reviewRepo.GetMyReviews();
            return View(new AccountDashboardModel(
                orders.Take(AccountDashboardModel.RecentOrderLimit).ToList(), orders.Count, wishlist.Count, reviews.Count));
        }

        public async Task<IActionResult> Reviews()
        {
            return View(await _reviewRepo.GetMyReviews());
        }
    }
}
