using BookShoppingCartMvcUI.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookShoppingCartMvcUI.Controllers;

// admin customer pages (read only): customer list + details with order history
[Authorize(Roles = nameof(Roles.Admin))]
public class CustomerController : Controller
{
    private readonly ICustomerRepository _customerRepository;

    public CustomerController(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<IActionResult> Index([FromQuery] AdminCustomerQuery query)
    {
        return View(await _customerRepository.GetCustomers(query));
    }

    public async Task<IActionResult> Details(string? id)
    {
        var customer = await _customerRepository.GetCustomer(id);
        if (customer is null)
            return NotFound();
        return View(customer);
    }
}
