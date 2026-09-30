namespace BookShoppingCartMvcUI.Shared;

// low stock rule in one place so the dashboard and inventory match
public static class StockLevel
{
    public const int LowStockThreshold = 5;

    // max copies per book. stops typos like 10000000 and keeps us far from int overflow
    public const int MaxQuantity = 100_000;

    public static bool IsLow(int quantity) => quantity <= LowStockThreshold;

    public static string Status(int quantity) => quantity switch
    {
        <= 0 => "Out of Stock",
        <= LowStockThreshold => "Low Stock",
        _ => "In Stock"
    };

    public static string BadgeClass(int quantity) => quantity switch
    {
        <= 0 => "text-bg-danger",
        <= LowStockThreshold => "text-bg-warning",
        _ => "text-bg-success"
    };
}
