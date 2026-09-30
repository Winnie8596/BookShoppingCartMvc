using BookShoppingCartMvcUI.Shared;
using Microsoft.EntityFrameworkCore;

namespace BookShoppingCartMvcUI.Repositories;

public class DashboardRepository : IDashboardRepository
{
    // these aren't sales, so they don't count as revenue even if paid
    public static readonly string[] NonRevenueStatuses = { "Cancelled", "Returned", "Refund" };

    private readonly ApplicationDbContext _db;

    public DashboardRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<AdminDashboardModel> GetDashboard()
    {
        // all the numbers are calculated in the db, only the short lists come back as rows
        var orders = _db.Orders.Where(o => !o.IsDeleted);

        // no Stock row = 0
        var stock = _db.Books.Select(b => new StockDisplayModel
        {
            BookId = b.Id,
            BookName = b.BookName,
            Quantity = b.Stock == null ? 0 : b.Stock.Quantity
        });
        var lowStock = stock.Where(s => s.Quantity <= StockLevel.LowStockThreshold);

        var customerRole = nameof(Roles.User);

        return new AdminDashboardModel
        {
            TotalBooks = await _db.Books.CountAsync(),
            TotalCustomers = await _db.UserRoles
                .Where(ur => _db.Roles.Any(r => r.Id == ur.RoleId && r.Name == customerRole))
                .Select(ur => ur.UserId)
                .Distinct()
                .CountAsync(),
            TotalOrders = await orders.CountAsync(),
            // Sum of no rows is NULL, hence the cast
            TotalRevenue = await _db.OrderDetails
                .Where(od => !od.Order.IsDeleted && od.Order.IsPaid
                    && !NonRevenueStatuses.Contains(od.Order.OrderStatus.StatusName))
                .SumAsync(od => (double?)(od.UnitPrice * od.Quantity)) ?? 0,
            LowStockCount = await lowStock.CountAsync(),
            OrdersByStatus = await orders
                .GroupBy(o => o.OrderStatus.StatusName)
                .Select(g => new StatusCountModel(g.Key, g.Count()))
                .ToListAsync(),
            LowStockBooks = await lowStock
                .OrderBy(s => s.Quantity).ThenBy(s => s.BookName)
                .Take(AdminDashboardModel.LowStockLimit)
                .ToListAsync(),
            RecentOrders = await orders
                .OrderByDescending(o => o.CreateDate).ThenByDescending(o => o.Id)
                .Take(AdminDashboardModel.RecentOrderLimit)
                .Select(o => new AdminRecentOrderModel(o.Id, o.CreateDate, o.Name, o.OrderStatus.StatusName, o.IsPaid,
                    o.OrderDetail.Sum(od => od.UnitPrice * od.Quantity)))
                .ToListAsync()
        };
    }
}

public interface IDashboardRepository
{
    Task<AdminDashboardModel> GetDashboard();
}
