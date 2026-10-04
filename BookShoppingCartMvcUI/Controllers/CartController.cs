using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookShoppingCartMvcUI.Controllers;

[Authorize]
public class CartController : Controller
{
    private readonly ICartRepository _cartRepo;

    public CartController(ICartRepository cartRepo)
    {
        _cartRepo = cartRepo;
    }
    // ajax add from the catalog, returns the new cart count
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddItem(int bookId, int qty = 1)
    {
        var result = await _cartRepo.TryAddItem(bookId, qty);
        if (!result.Succeeded)
            return BadRequest(new { message = result.ErrorMessage, cartCount = result.CartCount });
        return Ok(result.CartCount);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddFromDetails(int bookId, int qty)
    {
        var result = await _cartRepo.TryAddItem(bookId, qty);
        if (result.Succeeded)
            TempData["successMessage"] = $"Added {qty} to your cart.";
        else
            TempData["errorMessage"] = result.ErrorMessage;
        return RedirectToAction(nameof(HomeController.Details), "Home", new { id = bookId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveItem(int bookId)
    {
        await _cartRepo.RemoveItem(bookId);
        return RedirectToAction(nameof(GetUserCart));
    }

    // step: -1/1 from the -/+ buttons, 0 when they typed a number and clicked Update
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateQuantity(int bookId, int qty, int step = 0)
    {
        var result = await _cartRepo.UpdateQuantity(bookId, qty + Math.Clamp(step, -1, 1));
        if (!result.Succeeded)
            TempData["errorMessage"] = result.ErrorMessage;
        return RedirectToAction(nameof(GetUserCart));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveLine(int bookId)
    {
        await _cartRepo.RemoveLine(bookId);
        return RedirectToAction(nameof(GetUserCart));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ClearCart()
    {
        await _cartRepo.ClearCart();
        return RedirectToAction(nameof(GetUserCart));
    }

    public async Task<IActionResult> GetUserCart()
    {
        var cart = await _cartRepo.GetCartView();
        return View(cart);
    }

    public async Task<IActionResult> Checkout()
    {
        var cart = await _cartRepo.GetCartView();
        if (cart.HasStockProblems)
            return BackToCart(cart);
        ViewData["Cart"] = cart;
        // prefill with their login email
        var email = User.Identity?.Name;
        var model = new CheckoutModel { Email = email is { Length: <= 256 } && email.Contains('@') ? email : null };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(CheckoutModel model)
    {
        // stock might have changed since they opened the cart
        var cart = await _cartRepo.GetCartView();
        if (!cart.CanCheckout)
            return BackToCart(cart);
        if (!ModelState.IsValid)
        {
            ViewData["Cart"] = cart;
            return View(model);
        }
        // DoCheckout checks stock again inside the transaction anyway
        var result = await _cartRepo.DoCheckout(model);
        if (!result.Succeeded)
        {
            // the failure page says why, the cart is left as it was
            TempData["checkoutError"] = result.ErrorMessage;
            return RedirectToAction(nameof(OrderFailure));
        }
        // only shown once, a refresh just gets the normal message
        TempData["orderName"] = model.Name;
        TempData["orderPayment"] = model.PaymentMethod;
        return RedirectToAction(nameof(OrderSuccess));
    }

    private IActionResult BackToCart(CartViewModel cart)
    {
        TempData["errorMessage"] = cart.IsEmpty
            ? "Your cart is empty."
            : "Some items in your cart exceed the available stock. Please update your cart before checking out.";
        return RedirectToAction(nameof(GetUserCart));
    }

    public IActionResult OrderSuccess()
    {
        return View();
    }

    public IActionResult OrderFailure()
    {
        return View();
    }

}
