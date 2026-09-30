using BookShoppingCartMvcUI.Shared;
using Microsoft.EntityFrameworkCore;

namespace BookShoppingCartMvcUI.Repositories
{
    public class StockRepository : IStockRepository
    {
        private readonly ApplicationDbContext _context;

        public StockRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Stock?> GetStockByBookId(int bookId) => await _context.Stocks.FirstOrDefaultAsync(s => s.BookId == bookId);

        public async Task<StockDisplayModel?> GetStockItem(int bookId) =>
            await StockItems().FirstOrDefaultAsync(s => s.BookId == bookId);

        // both of these are single conditional UPDATEs (like checkout). reading, changing in C# and saving back
        // could overwrite a sale that happened in between

        public async Task<StockChangeResult> SetStock(int bookId, int quantity, int? expectedQuantity = null)
        {
            if (quantity < 0 || quantity > StockLevel.MaxQuantity)
                return await Result(StockChangeStatus.InvalidQuantity, bookId);
            if (!await _context.Books.AnyAsync(b => b.Id == bookId))
                return new StockChangeResult(StockChangeStatus.BookNotFound, 0);

            var rows = _context.Stocks.Where(s => s.BookId == bookId);
            if (expectedQuantity is int expected)
                rows = rows.Where(s => s.Quantity == expected);
            int updated = await rows.ExecuteUpdateAsync(s => s.SetProperty(x => x.Quantity, quantity));
            if (updated > 0)
                return new StockChangeResult(StockChangeStatus.Updated, quantity);

            // nothing updated: no Stock row yet (counts as 0) or the count changed
            if (await _context.Stocks.AnyAsync(s => s.BookId == bookId) || expectedQuantity is > 0)
                return await Result(StockChangeStatus.StockChanged, bookId);
            return await CreateStock(bookId, quantity);
        }

        public async Task<StockChangeResult> AdjustStock(int bookId, int change)
        {
            // not Math.Abs, it throws for int.MinValue
            if (change == 0 || change < -StockLevel.MaxQuantity || change > StockLevel.MaxQuantity)
                return await Result(StockChangeStatus.InvalidQuantity, bookId);
            if (!await _context.Books.AnyAsync(b => b.Id == bookId))
                return new StockChangeResult(StockChangeStatus.BookNotFound, 0);

            // relative change so no expected count needed, a sale in between is just kept.
            // the bounds don't add to Quantity so the sql can't overflow
            int updated = await _context.Stocks
                .Where(s => s.BookId == bookId && s.Quantity >= -change && s.Quantity <= StockLevel.MaxQuantity - change)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.Quantity, x => x.Quantity + change));
            if (updated > 0)
                return await Result(StockChangeStatus.Updated, bookId);

            var current = await _context.Stocks.Where(s => s.BookId == bookId).Select(s => (int?)s.Quantity).FirstOrDefaultAsync();
            if (current is null)
                return change > 0 ? await CreateStock(bookId, change) : new StockChangeResult(StockChangeStatus.BelowZero, 0);
            return new StockChangeResult(current + change < 0 ? StockChangeStatus.BelowZero : StockChangeStatus.AboveMaximum, current.Value);
        }

        public async Task<InventoryListModel> GetInventory(InventoryQuery query)
        {
            var search = query.Search?.Trim();
            var status = InventoryQuery.Statuses.Contains(query.Status) ? query.Status! : InventoryQuery.Statuses[0];
            var sort = InventoryQuery.Sorts.Contains(query.Sort) ? query.Sort! : InventoryQuery.Sorts[0];
            var pageSize = query.PageSize < 1 ? InventoryQuery.DefaultPageSize : Math.Min(query.PageSize, InventoryQuery.MaxPageSize);
            var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;

            var items = StockItems();
            if (!string.IsNullOrEmpty(search))
            {
                // ToLower so it's case insensitive whatever the collation is
                var term = search.ToLower();
                items = items.Where(s => s.BookName!.ToLower().Contains(term) || s.AuthorName!.ToLower().Contains(term));
            }

            const int low = StockLevel.LowStockThreshold;
            var model = new InventoryListModel
            {
                Search = search,
                Status = status,
                Sort = sort,
                PageNumber = pageNumber,
                PageSize = pageSize,
                OutOfStockCount = await items.CountAsync(s => s.Quantity <= 0),
                LowStockCount = await items.CountAsync(s => s.Quantity > 0 && s.Quantity <= low),
                InStockCount = await items.CountAsync(s => s.Quantity > low)
            };

            items = status switch
            {
                "out" => items.Where(s => s.Quantity <= 0),
                "low" => items.Where(s => s.Quantity > 0 && s.Quantity <= low),
                "in" => items.Where(s => s.Quantity > low),
                _ => items
            };
            model.TotalCount = status switch
            {
                "out" => model.OutOfStockCount,
                "low" => model.LowStockCount,
                "in" => model.InStockCount,
                _ => model.OutOfStockCount + model.LowStockCount + model.InStockCount
            };

            // long so a huge page number can't overflow
            long skip = (long)(pageNumber - 1) * pageSize;
            if (skip >= model.TotalCount)
                return model;

            // BookId as tie breaker so paging is stable
            items = sort switch
            {
                "stock_desc" => items.OrderByDescending(s => s.Quantity).ThenBy(s => s.BookId),
                "title" => items.OrderBy(s => s.BookName).ThenBy(s => s.BookId),
                _ => items.OrderBy(s => s.Quantity).ThenBy(s => s.BookName).ThenBy(s => s.BookId)
            };
            model.Items = await items.Skip((int)skip).Take(pageSize).ToListAsync();
            return model;
        }

        // all books, including ones with no Stock row (count as 0)
        private IQueryable<StockDisplayModel> StockItems() => _context.Books.Select(b => new StockDisplayModel
        {
            Id = b.Stock == null ? 0 : b.Stock.Id,
            BookId = b.Id,
            BookName = b.BookName,
            AuthorName = b.AuthorName,
            Quantity = b.Stock == null ? 0 : b.Stock.Quantity
        });

        private async Task<StockChangeResult> CreateStock(int bookId, int quantity)
        {
            var stock = new Stock { BookId = bookId, Quantity = quantity };
            _context.Stocks.Add(stock);
            try
            {
                await _context.SaveChangesAsync();
                return new StockChangeResult(StockChangeStatus.Updated, quantity);
            }
            catch (DbUpdateException)
            {
                // unique index on BookId - another request created it first
                _context.Entry(stock).State = EntityState.Detached;
                return await Result(StockChangeStatus.StockChanged, bookId);
            }
        }

        private async Task<StockChangeResult> Result(StockChangeStatus status, int bookId) =>
            new(status, await _context.Stocks.Where(s => s.BookId == bookId).Select(s => s.Quantity).FirstOrDefaultAsync());
    }

    public interface IStockRepository
    {
        Task<InventoryListModel> GetInventory(InventoryQuery query);
        Task<Stock?> GetStockByBookId(int bookId);
        // book + stock count, null if the book doesn't exist
        Task<StockDisplayModel?> GetStockItem(int bookId);
        // set the count. if expectedQuantity is given, only if it's still that
        Task<StockChangeResult> SetStock(int bookId, int quantity, int? expectedQuantity = null);
        // add/remove copies, won't go below 0 or over the max
        Task<StockChangeResult> AdjustStock(int bookId, int change);
    }
}
