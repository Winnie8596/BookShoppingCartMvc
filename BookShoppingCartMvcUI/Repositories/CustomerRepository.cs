using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BookShoppingCartMvcUI.Repositories;

// read only customer data for the admin pages. everything is projected into the Admin*Customer models
// so PasswordHash, SecurityStamp etc are never selected
public class CustomerRepository : ICustomerRepository
{
    private readonly ApplicationDbContext _db;

    public CustomerRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<AdminCustomersPageModel> GetCustomers(AdminCustomerQuery query)
    {
        var search = query.Search?.Trim();
        var sort = AdminCustomerQuery.Sorts.Contains(query.Sort) ? query.Sort! : AdminCustomerQuery.Sorts[0];
        var pageSize = query.PageSize < 1 ? AdminCustomerQuery.DefaultPageSize : Math.Min(query.PageSize, AdminCustomerQuery.MaxPageSize);
        var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;

        var customers = Customers();
        if (!string.IsNullOrEmpty(search))
        {
            // ToLower for case insensitive search. identity users have no name so search the checkout names too
            var term = search.ToLower();
            customers = customers.Where(u => u.Email!.ToLower().Contains(term)
                || u.UserName!.ToLower().Contains(term)
                || u.PhoneNumber!.Contains(search)
                || _db.Orders.Any(o => o.UserId == u.Id && !o.IsDeleted && o.Name!.ToLower().Contains(term)));
        }

        var page = new AdminCustomersPageModel
        {
            Search = search,
            Sort = sort,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = await customers.CountAsync()
        };

        // long so a huge page number can't overflow
        long skip = (long)(pageNumber - 1) * pageSize;
        if (skip >= page.TotalCount)
            return page;

        // subqueries so the db only works these out for the customers on this page
        var revenueLines = RevenueLines();
        var rows = customers.Select(u => new AdminCustomerRowModel
        {
            Id = u.Id,
            Email = u.Email,
            EmailConfirmed = u.EmailConfirmed,
            LockoutEnd = u.LockoutEnd,
            LatestOrderName = _db.Orders.Where(o => o.UserId == u.Id && !o.IsDeleted)
                .OrderByDescending(o => o.CreateDate).ThenByDescending(o => o.Id).Select(o => o.Name).FirstOrDefault(),
            OrderCount = _db.Orders.Count(o => o.UserId == u.Id && !o.IsDeleted),
            LastOrderDate = _db.Orders.Where(o => o.UserId == u.Id && !o.IsDeleted).Max(o => (DateTime?)o.CreateDate),
            // Sum of no rows is NULL, hence the cast
            TotalSpent = revenueLines.Where(od => od.Order.UserId == u.Id).Sum(od => (double?)(od.UnitPrice * od.Quantity)) ?? 0
        });
        IOrderedQueryable<AdminCustomerRowModel> sorted = sort switch
        {
            "recent" => rows.OrderByDescending(r => r.LastOrderDate).ThenBy(r => r.Email),
            "orders" => rows.OrderByDescending(r => r.OrderCount).ThenBy(r => r.Email),
            "spent" => rows.OrderByDescending(r => r.TotalSpent).ThenBy(r => r.Email),
            _ => rows.OrderBy(r => r.Email)
        };
        page.Customers = await sorted.ThenBy(r => r.Id)
            .Skip((int)skip)
            .Take(pageSize)
            .ToListAsync();
        return page;
    }

    public async Task<AdminCustomerDetailsModel?> GetCustomer(string? id)
    {
        if (string.IsNullOrEmpty(id))
            return null;

        // admin/non-customer id returns nothing, same as an unknown id
        var revenueLines = RevenueLines();
        var customer = await Customers()
            .Where(u => u.Id == id)
            .Select(u => new AdminCustomerDetailsModel
            {
                Id = u.Id,
                Email = u.Email,
                UserName = u.UserName,
                PhoneNumber = u.PhoneNumber,
                EmailConfirmed = u.EmailConfirmed,
                TwoFactorEnabled = u.TwoFactorEnabled,
                LockoutEnd = u.LockoutEnd,
                AccessFailedCount = u.AccessFailedCount,
                WishlistCount = _db.WishlistItems.Count(w => w.UserId == u.Id),
                ReviewCount = _db.Reviews.Count(r => r.UserId == u.Id),
                TotalSpent = revenueLines.Where(od => od.Order.UserId == u.Id).Sum(od => (double?)(od.UnitPrice * od.Quantity)) ?? 0
            })
            .FirstOrDefaultAsync();
        if (customer is null)
            return null;

        var orders = _db.Orders.Where(o => o.UserId == id && !o.IsDeleted)
            .OrderByDescending(o => o.CreateDate).ThenByDescending(o => o.Id);
        customer.Orders = await orders
            .Select(o => new OrderSummaryModel(o.Id, o.CreateDate, o.OrderStatus.StatusName, o.PaymentMethod, o.IsPaid,
                o.OrderDetail.Sum(od => od.Quantity),
                o.OrderDetail.Sum(od => od.UnitPrice * od.Quantity)))
            .ToListAsync();
        if (customer.Orders.Count > 0)
            customer.LatestDelivery = await orders
                .Select(o => new AdminCustomerDeliveryModel(o.Name, o.MobileNumber, o.Address))
                .FirstAsync();
        return customer;
    }

    public Task<bool> IsCustomer(string? id) =>
        string.IsNullOrEmpty(id) ? Task.FromResult(false) : Customers().AnyAsync(u => u.Id == id);

    // users in the customer role (same as the dashboard)
    private IQueryable<IdentityUser> Customers()
    {
        var customerRole = nameof(Roles.User);
        return _db.Users.Where(u => _db.UserRoles.Any(ur => ur.UserId == u.Id
            && _db.Roles.Any(r => r.Id == ur.RoleId && r.Name == customerRole)));
    }

    // order lines that count as spent: paid, not deleted, not cancelled/returned/refunded
    private IQueryable<OrderDetail> RevenueLines() =>
        _db.OrderDetails.Where(od => !od.Order.IsDeleted && od.Order.IsPaid
            && !DashboardRepository.NonRevenueStatuses.Contains(od.Order.OrderStatus.StatusName));
}

public interface ICustomerRepository
{
    // one page of customers
    Task<AdminCustomersPageModel> GetCustomers(AdminCustomerQuery query);
    // customer + order history, null if not found or not a customer
    Task<AdminCustomerDetailsModel?> GetCustomer(string? id);
    // is this a customer account (for linking from orders)
    Task<bool> IsCustomer(string? id);
}
