using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookShoppingCartMvcUI.Controllers
{
    // no user id in the routes, the repo uses the logged in user
    [Authorize]
    public class WishlistController : Controller
    {
        private readonly IWishlistRepository _wishlistRepo;

        public WishlistController(IWishlistRepository wishlistRepo)
        {
            _wishlistRepo = wishlistRepo;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _wishlistRepo.GetWishlist());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int bookId, string? returnUrl = null)
        {
            var result = await _wishlistRepo.Add(bookId);
            if (result.Succeeded)
                TempData["successMessage"] = "Saved to your wishlist.";
            else
                TempData["errorMessage"] = result.ErrorMessage;
            return BackTo(returnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int bookId, string? returnUrl = null)
        {
            await _wishlistRepo.Remove(bookId);
            TempData["successMessage"] = "Removed from your wishlist.";
            return BackTo(returnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MoveToCart(int bookId)
        {
            var result = await _wishlistRepo.MoveToCart(bookId);
            if (result.Succeeded)
                TempData["successMessage"] = "Moved to your cart.";
            else
                TempData["errorMessage"] = result.ErrorMessage;
            return RedirectToAction(nameof(Index));
        }

        // only redirect to local urls (no open redirect)
        private IActionResult BackTo(string? returnUrl) =>
            Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : RedirectToAction(nameof(Index));
    }
}
