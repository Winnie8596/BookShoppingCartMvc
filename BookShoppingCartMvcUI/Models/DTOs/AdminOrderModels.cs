namespace BookShoppingCartMvcUI.Models.DTOs;

// filters for the admin order list, bad values fall back to the defaults
public class AdminOrderQuery
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
    public static readonly string[] PaymentFilters = { "all", "paid", "unpaid" };

    // order number (# optional), or customer name/email/mobile
    public string? Search { get; set; }
    // status name, empty = all
    public string? Status { get; set; }
    public string? Payment { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = DefaultPageSize;
}

// one page of orders (newest first) + status counts + filters
public class AdminOrdersPageModel
{
    public List<AdminOrderRowModel> Orders { get; set; } = new();
    public string? Search { get; set; }
    // null = all statuses
    public string? Status { get; set; }
    public string Payment { get; set; } = "all";
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = AdminOrderQuery.DefaultPageSize;
    // matching all the filters
    public int TotalCount { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;
    // per status, only counts the search + payment filter so the tabs make sense
    public Dictionary<string, int> StatusCounts { get; set; } = new();
    public int AllStatusesCount => StatusCounts.Values.Sum();
}

// just the columns the list needs
public class AdminOrderRowModel
{
    public int Id { get; set; }
    public DateTime CreateDate { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? PaymentMethod { get; set; }
    public bool IsPaid { get; set; }
    public string? StatusName { get; set; }
    public int ItemCount { get; set; }
    public double Total { get; set; }
}

// everything on the admin order page
public class AdminOrderDetailsModel
{
    public int Id { get; set; }
    public DateTime CreateDate { get; set; }
    // user who placed the order
    public string? UserId { get; set; }
    // true if the account still exists (for the link)
    public bool HasCustomerAccount { get; set; }
    public int OrderStatusId { get; set; }
    public string? StatusName { get; set; }
    public string? PaymentMethod { get; set; }
    public bool IsPaid { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? MobileNumber { get; set; }
    public string? Address { get; set; }
    public List<AdminOrderLineModel> Lines { get; set; } = new();
    public double Total => Lines.Sum(l => l.Subtotal);
    // statuses it can move to next, empty once it's finished
    public List<OrderStatus> NextStatuses { get; set; } = new();
}

// order line, unit price from when it was ordered
public record AdminOrderLineModel(int BookId, string? BookName, string? GenreName, int Quantity, double UnitPrice)
{
    public double Subtotal => UnitPrice * Quantity;
}

public enum OrderStatusChangeStatus
{
    Updated,
    OrderNotFound,
    // status doesn't exist
    UnknownStatus,
    // not allowed by the workflow, e.g. Delivered -> Pending
    NotAllowed,
    // someone else changed it while the page was open
    StatusChanged
}

// result of a status change + the current status
public record OrderStatusChangeResult(OrderStatusChangeStatus Status, string? CurrentStatus = null, string? NewStatus = null)
{
    public bool Succeeded => Status == OrderStatusChangeStatus.Updated;
}
