namespace BookShoppingCartMvcUI.Models.DTOs;

// dashboard numbers + the lists that need attention
public class AdminDashboardModel
{
    public const int RecentOrderLimit = 5;
    public const int LowStockLimit = 10;

    public int TotalBooks { get; set; }
    // users in the "User" role (admin not counted)
    public int TotalCustomers { get; set; }
    // all non-deleted orders, any status
    public int TotalOrders { get; set; }
    // paid orders that weren't cancelled/returned/refunded
    public double TotalRevenue { get; set; }
    // books at or below the low stock threshold (incl. out of stock)
    public int LowStockCount { get; set; }

    public List<StatusCountModel> OrdersByStatus { get; set; } = new();
    // lowest stock first, max LowStockLimit
    public List<StockDisplayModel> LowStockBooks { get; set; } = new();
    public List<AdminRecentOrderModel> RecentOrders { get; set; } = new();
}

public record StatusCountModel(string? StatusName, int Count);

public record AdminRecentOrderModel(int Id, DateTime CreateDate, string? Name, string? StatusName, bool IsPaid, double Total);
