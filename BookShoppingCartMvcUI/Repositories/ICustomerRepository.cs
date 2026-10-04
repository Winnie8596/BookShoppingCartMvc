namespace BookShoppingCartMvcUI.Repositories;

public interface ICustomerRepository
{
    // one page of customers
    Task<AdminCustomersPageModel> GetCustomers(AdminCustomerQuery query);
    // customer + order history, null if not found or not a customer
    Task<AdminCustomerDetailsModel?> GetCustomer(string? id);
    // is this a customer account (for linking from orders)
    Task<bool> IsCustomer(string? id);
}
