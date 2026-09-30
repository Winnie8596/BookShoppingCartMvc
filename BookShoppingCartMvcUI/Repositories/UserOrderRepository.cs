using BookShoppingCartMvcUI.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BookShoppingCartMvcUI.Repositories
{
    public class UserOrderRepository : IUserOrderRepository
    {
        private readonly ApplicationDbContext _db;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<IdentityUser> _userManager;


        public UserOrderRepository(ApplicationDbContext db,
            UserManager<IdentityUser> userManager,
             IHttpContextAccessor httpContextAccessor)
        {
            _db = db;
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
        }

        public async Task<AdminOrdersPageModel> GetAllOrders(AdminOrderQuery query)
        {
            var search = query.Search?.Trim();
            var payment = AdminOrderQuery.PaymentFilters.Contains(query.Payment) ? query.Payment! : AdminOrderQuery.PaymentFilters[0];
            var pageSize = query.PageSize < 1 ? AdminOrderQuery.DefaultPageSize : Math.Min(query.PageSize, AdminOrderQuery.MaxPageSize);
            var pageNumber = query.PageNumber < 1 ? 1 : query.PageNumber;

            var orders = _db.Orders.Where(o => !o.IsDeleted);
            if (!string.IsNullOrEmpty(search))
            {
                // "#12" = order 12 only, a plain "12" can also match a mobile number.
                // ToLower for case insensitive
                var term = search.ToLower();
                if (search.StartsWith('#'))
                {
                    int id = int.TryParse(search[1..].Trim(), out var parsed) ? parsed : 0;
                    orders = orders.Where(o => o.Id == id);
                }
                else if (int.TryParse(search, out var orderId))
                    orders = orders.Where(o => o.Id == orderId || o.MobileNumber!.Contains(search));
                else
                    orders = orders.Where(o => o.Name!.ToLower().Contains(term) || o.Email!.ToLower().Contains(term)
                        || o.MobileNumber!.Contains(search));
            }
            orders = payment switch
            {
                "paid" => orders.Where(o => o.IsPaid),
                "unpaid" => orders.Where(o => !o.IsPaid),
                _ => orders
            };

            // every status gets a tab even with 0 orders, one GROUP BY for the counts
            var counts = OrderWorkflow.Statuses.ToDictionary(s => s, _ => 0);
            foreach (var row in await orders.GroupBy(o => o.OrderStatus.StatusName)
                         .Select(g => new { Status = g.Key, Count = g.Count() }).ToListAsync())
                counts[row.Status ?? ""] = row.Count;

            var status = !string.IsNullOrEmpty(query.Status) && counts.ContainsKey(query.Status) ? query.Status : null;
            var page = new AdminOrdersPageModel
            {
                Search = search,
                Status = status,
                Payment = payment,
                PageNumber = pageNumber,
                PageSize = pageSize,
                StatusCounts = counts,
                TotalCount = status is null ? counts.Values.Sum() : counts[status]
            };

            // long so a huge page number can't overflow
            long skip = (long)(pageNumber - 1) * pageSize;
            if (skip >= page.TotalCount)
                return page;

            if (status is not null)
                orders = orders.Where(o => o.OrderStatus.StatusName == status);
            // sort + paging in sql, item count and total are calculated by the db
            page.Orders = await orders
                .OrderByDescending(o => o.CreateDate).ThenByDescending(o => o.Id)
                .Skip((int)skip)
                .Take(pageSize)
                .Select(o => new AdminOrderRowModel
                {
                    Id = o.Id,
                    CreateDate = o.CreateDate,
                    Name = o.Name,
                    Email = o.Email,
                    PaymentMethod = o.PaymentMethod,
                    IsPaid = o.IsPaid,
                    StatusName = o.OrderStatus.StatusName,
                    ItemCount = o.OrderDetail.Sum(od => od.Quantity),
                    Total = o.OrderDetail.Sum(od => od.UnitPrice * od.Quantity)
                })
                .ToListAsync();
            return page;
        }

        public async Task<AdminOrderDetailsModel?> GetAdminOrder(int orderId)
        {
            var order = await _db.Orders
                .Where(o => o.Id == orderId && !o.IsDeleted)
                .Select(o => new AdminOrderDetailsModel
                {
                    Id = o.Id,
                    CreateDate = o.CreateDate,
                    UserId = o.UserId,
                    OrderStatusId = o.OrderStatusId,
                    StatusName = o.OrderStatus.StatusName,
                    PaymentMethod = o.PaymentMethod,
                    IsPaid = o.IsPaid,
                    Name = o.Name,
                    Email = o.Email,
                    MobileNumber = o.MobileNumber,
                    Address = o.Address,
                    Lines = o.OrderDetail
                        .OrderBy(od => od.Id)
                        .Select(od => new AdminOrderLineModel(od.BookId, od.Book.BookName, od.Book.Genre.GenreName, od.Quantity, od.UnitPrice))
                        .ToList()
                })
                .FirstOrDefaultAsync();
            if (order is null)
                return null;

            var next = OrderWorkflow.NextStatuses(order.StatusName).ToList();
            if (next.Count > 0)
            {
                var statuses = await _db.orderStatuses.AsNoTracking().Where(s => next.Contains(s.StatusName!)).ToListAsync();
                order.NextStatuses = statuses.OrderBy(s => next.IndexOf(s.StatusName!)).ToList();
            }
            return order;
        }

        public async Task<OrderStatusChangeResult> ChangeOrderStatus(int orderId, int newStatusId, int? expectedStatusId = null)
        {
            var current = await CurrentStatus(orderId);
            if (current is null)
                return new OrderStatusChangeResult(OrderStatusChangeStatus.OrderNotFound);
            if (expectedStatusId is int expected && expected != current.Id)
                return new OrderStatusChangeResult(OrderStatusChangeStatus.StatusChanged, current.StatusName);
            var target = await _db.orderStatuses.AsNoTracking().FirstOrDefaultAsync(s => s.Id == newStatusId);
            if (target is null)
                return new OrderStatusChangeResult(OrderStatusChangeStatus.UnknownStatus, current.StatusName);
            if (!OrderWorkflow.CanMove(current.StatusName, target.StatusName))
                return new OrderStatusChangeResult(OrderStatusChangeStatus.NotAllowed, current.StatusName, target.StatusName);

            // status change + restock go together - a cancelled order has to give its books back,
            // and if the restock fails the order shouldn't stay cancelled
            await using var transaction = await _db.Database.BeginTransactionAsync();

            // conditional UPDATE like the stock ones: if another admin changed it first nothing happens,
            // so an order can't be cancelled (and restocked) twice
            int updated = await _db.Orders
                .Where(o => o.Id == orderId && o.OrderStatusId == current.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(o => o.OrderStatusId, target.Id));
            if (updated == 0)
                return new OrderStatusChangeResult(OrderStatusChangeStatus.StatusChanged, (await CurrentStatus(orderId))?.StatusName);

            if (OrderWorkflow.RestocksOnEntry(target.StatusName))
            {
                // one update per book in book id order (same as checkout) to avoid deadlocks
                var books = await _db.OrderDetails
                    .Where(od => od.OrderId == orderId)
                    .GroupBy(od => od.BookId)
                    .Select(g => new { BookId = g.Key, Quantity = g.Sum(od => od.Quantity) })
                    .OrderBy(b => b.BookId)
                    .ToListAsync();
                foreach (var book in books)
                {
                    int restocked = await _db.Stocks
                        .Where(s => s.BookId == book.BookId)
                        .ExecuteUpdateAsync(s => s.SetProperty(x => x.Quantity, x => x.Quantity + book.Quantity));
                    if (restocked == 0)
                        _db.Stocks.Add(new Stock { BookId = book.BookId, Quantity = book.Quantity });
                }
                await _db.SaveChangesAsync();
            }

            await transaction.CommitAsync();
            return new OrderStatusChangeResult(OrderStatusChangeStatus.Updated, target.StatusName, target.StatusName);
        }

        public async Task<bool> TogglePaymentStatus(int orderId)
        {
            // flip it in the db so 2 quick clicks = 2 flips
            int updated = await _db.Orders
                .Where(o => o.Id == orderId && !o.IsDeleted)
                .ExecuteUpdateAsync(s => s.SetProperty(o => o.IsPaid, o => !o.IsPaid));
            return updated > 0;
        }

        private Task<OrderStatus?> CurrentStatus(int orderId) =>
            _db.Orders.Where(o => o.Id == orderId && !o.IsDeleted).Select(o => o.OrderStatus).AsNoTracking().FirstOrDefaultAsync();

        public async Task<List<OrderSummaryModel>> GetMyOrders()
        {
            return await MyOrders()
                .OrderByDescending(o => o.CreateDate).ThenByDescending(o => o.Id)
                .Select(o => new OrderSummaryModel(o.Id, o.CreateDate, o.OrderStatus.StatusName, o.PaymentMethod, o.IsPaid,
                    o.OrderDetail.Sum(od => od.Quantity),
                    o.OrderDetail.Sum(od => od.UnitPrice * od.Quantity)))
                .ToListAsync();
        }

        public async Task<OrderDetailsModel?> GetMyOrder(int orderId)
        {
            // owner is in the where so someone else's order id finds nothing
            return await MyOrders()
                .Where(o => o.Id == orderId)
                .Select(o => new OrderDetailsModel
                {
                    Id = o.Id,
                    CreateDate = o.CreateDate,
                    StatusName = o.OrderStatus.StatusName,
                    PaymentMethod = o.PaymentMethod,
                    IsPaid = o.IsPaid,
                    Name = o.Name,
                    Email = o.Email,
                    MobileNumber = o.MobileNumber,
                    Address = o.Address,
                    Lines = o.OrderDetail
                        .OrderBy(od => od.Id)
                        .Select(od => new OrderLineModel(od.BookId, od.Book.BookName, od.Book.AuthorName, od.Book.Image,
                            od.Quantity, od.UnitPrice))
                        .ToList()
                })
                .FirstOrDefaultAsync();
        }

        // user's non-deleted orders, none if not logged in
        private IQueryable<Order> MyOrders()
        {
            var userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
                return _db.Orders.Where(o => false);
            return _db.Orders.Where(o => o.UserId == userId && !o.IsDeleted);
        }

        private string GetUserId()
        {
            var principal = _httpContextAccessor.HttpContext.User;
            string userId = _userManager.GetUserId(principal);
            return userId;
        }
    }
}
