namespace BookShoppingCartMvcUI.Repositories;

public interface IUserOrderRepository
{
    // admin order list, newest first
    Task<AdminOrdersPageModel> GetAllOrders(AdminOrderQuery query);
    // one order for the admin page, null if not found
    Task<AdminOrderDetailsModel?> GetAdminOrder(int orderId);
    // change the status if the workflow allows it. if expectedStatusId is given, only if it's still in that status.
    // cancelling puts the books back in stock in the same transaction
    Task<OrderStatusChangeResult> ChangeOrderStatus(int orderId, int newStatusId, int? expectedStatusId = null);
    // toggle paid, false if the order doesn't exist
    Task<bool> TogglePaymentStatus(int orderId);
    // user's orders, newest first. limit = only the latest few
    Task<List<OrderSummaryModel>> GetMyOrders(int? limit = null);
    Task<int> CountMyOrders();
    // one of the user's orders, null if not found or not theirs
    Task<OrderDetailsModel?> GetMyOrder(int orderId);

}
