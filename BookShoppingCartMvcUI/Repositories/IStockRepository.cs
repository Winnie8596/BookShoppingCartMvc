namespace BookShoppingCartMvcUI.Repositories;

public interface IStockRepository
{
    Task<InventoryListModel> GetInventory(InventoryQuery query);
    // book + stock count, null if the book doesn't exist
    Task<StockDisplayModel?> GetStockItem(int bookId);
    // set the count. if expectedQuantity is given, only if it's still that
    Task<StockChangeResult> SetStock(int bookId, int quantity, int? expectedQuantity = null);
    // add/remove copies, won't go below 0 or over the max
    Task<StockChangeResult> AdjustStock(int bookId, int change);
}
