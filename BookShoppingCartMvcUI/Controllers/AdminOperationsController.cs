using BookShoppingCartMvcUI.Constants;
using BookShoppingCartMvcUI.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookShoppingCartMvcUI.Controllers;

[Authorize(Roles = nameof(Roles.Admin))]
public class AdminOperationsController : Controller
{
    private readonly IUserOrderRepository _userOrderRepository;
    private readonly IDashboardRepository _dashboardRepository;
    private readonly ICustomerRepository _customerRepository;
    public AdminOperationsController(IUserOrderRepository userOrderRepository, IDashboardRepository dashboardRepository,
        ICustomerRepository customerRepository)
    {
        _userOrderRepository = userOrderRepository;
        _dashboardRepository = dashboardRepository;
        _customerRepository = customerRepository;
    }

    public async Task<IActionResult> AllOrders([FromQuery] AdminOrderQuery query)
    {
        return View(await _userOrderRepository.GetAllOrders(query));
    }

    public async Task<IActionResult> OrderDetails(int id)
    {
        var order = await _userOrderRepository.GetAdminOrder(id);
        if (order is null)
            return NotFound();
        // order might be from a deleted account, so only link if the customer still exists
        order.HasCustomerAccount = await _customerRepository.IsCustomer(order.UserId);
        return View(order);
    }

    [HttpPost]
    public async Task<IActionResult> TogglePaymentStatus(int orderId)
    {
        if (!await _userOrderRepository.TogglePaymentStatus(orderId))
            return NotFound();
        TempData["successMessage"] = $"Payment status of order #{orderId} updated.";
        return RedirectToAction(nameof(OrderDetails), new { id = orderId });
    }

    // old status page, just redirects to the order page now
    [HttpGet]
    public IActionResult UpdateOrderStatus(int orderId) => RedirectToAction(nameof(OrderDetails), new { id = orderId });

    [HttpPost]
    public async Task<IActionResult> UpdateOrderStatus(UpdateOrderStatusModel data)
    {
        if (!ModelState.IsValid)
        {
            TempData["errorMessage"] = "Choose a new status.";
            return RedirectToAction(nameof(OrderDetails), new { id = data.OrderId });
        }

        var result = await _userOrderRepository.ChangeOrderStatus(data.OrderId, data.OrderStatusId!.Value, data.ExpectedStatusId);
        switch (result.Status)
        {
            case OrderStatusChangeStatus.OrderNotFound:
                return NotFound();
            case OrderStatusChangeStatus.Updated:
                TempData["successMessage"] = result.NewStatus == OrderWorkflow.Cancelled
                    ? $"Order #{data.OrderId} is now Cancelled. Its books are back in stock."
                    : $"Order #{data.OrderId} is now {result.NewStatus}.";
                break;
            case OrderStatusChangeStatus.StatusChanged:
                TempData["errorMessage"] = $"Order #{data.OrderId} was changed to {result.CurrentStatus} while this page was open. Check it and try again.";
                break;
            case OrderStatusChangeStatus.NotAllowed:
                TempData["errorMessage"] = $"A {result.CurrentStatus} order cannot be changed to {result.NewStatus}.";
                break;
            default:
                TempData["errorMessage"] = "That status does not exist.";
                break;
        }
        return RedirectToAction(nameof(OrderDetails), new { id = data.OrderId });
    }


    public async Task<IActionResult> Dashboard()
    {
        return View(await _dashboardRepository.GetDashboard());
    }

}
