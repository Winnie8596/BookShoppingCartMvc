using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookShoppingCartMvcUI.Controllers;

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
        // these only return the logged in user's stuff. counts are done in sql, no need to load every row
        var recentOrders = await _orderRepo.GetMyOrders(AccountDashboardModel.RecentOrderLimit);
        return View(new AccountDashboardModel(recentOrders, await _orderRepo.CountMyOrders(),
            await _wishlistRepo.Count(), await _reviewRepo.CountMyReviews()));
    }

    public async Task<IActionResult> Reviews()
    {
        return View(await _reviewRepo.GetMyReviews());
    }
}
