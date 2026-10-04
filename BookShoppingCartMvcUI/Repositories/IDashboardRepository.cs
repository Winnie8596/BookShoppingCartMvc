namespace BookShoppingCartMvcUI.Repositories;

public interface IDashboardRepository
{
    Task<AdminDashboardModel> GetDashboard();
}
