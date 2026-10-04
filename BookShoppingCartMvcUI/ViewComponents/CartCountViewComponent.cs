using Microsoft.AspNetCore.Mvc;

namespace BookShoppingCartMvcUI.ViewComponents;

// cart count in the navbar. rendered server side so it's right straight away - the old layout
// called /Cart/GetTotalItemInCart after every load, which flashed 0 and sent guests to the login page
public class CartCountViewComponent : ViewComponent
{
    private readonly ICartRepository _cartRepo;

    public CartCountViewComponent(ICartRepository cartRepo)
    {
        _cartRepo = cartRepo;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        int count = User.Identity?.IsAuthenticated == true ? await _cartRepo.GetCartItemCount() : 0;
        return View(count);
    }
}
