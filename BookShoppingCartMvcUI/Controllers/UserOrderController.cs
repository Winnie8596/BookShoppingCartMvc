using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookShoppingCartMvcUI.Controllers;

// repo only returns the logged in user's orders
[Authorize]
public class UserOrderController : Controller
{
    private readonly IUserOrderRepository _userOrderRepo;

    public UserOrderController(IUserOrderRepository userOrderRepo)
    {
        _userOrderRepo = userOrderRepo;
    }
    public async Task<IActionResult> UserOrders()
    {
        var orders = await _userOrderRepo.GetMyOrders();
        return View(orders);
    }

    public async Task<IActionResult> Details(int id)
    {
        // same 404 for someone else's order as a missing one, so ids can't be guessed
        var order = await _userOrderRepo.GetMyOrder(id);
        return order is null ? NotFound() : View(order);
    }
}
