namespace BookShoppingCartMvcUI.Models.DTOs
{
    // My account page: latest orders + counts for each section
    public record AccountDashboardModel(IReadOnlyList<OrderSummaryModel> RecentOrders, int OrderCount, int WishlistCount, int ReviewCount)
    {
        public const int RecentOrderLimit = 3;
    }
}
