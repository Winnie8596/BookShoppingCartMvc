namespace BookShoppingCartMvcUI.Models.DTOs;

// the admin customer pages only use these models. the queries pick the columns by name
// so the password hash, security stamp etc never even get loaded

// filters for the customer list, bad values fall back to the defaults
public class AdminCustomerQuery
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;
    public static readonly IReadOnlyList<string> Sorts = ["email", "recent", "orders", "spent"];

    // email, username, phone, or the name they used on an order
    public string? Search { get; set; }
    public string? Sort { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = DefaultPageSize;
}

// one page of customers + the filters
public class AdminCustomersPageModel : PagedResult<AdminCustomerRowModel>
{
    public AdminCustomersPageModel() : base(AdminCustomerQuery.DefaultPageSize) { }

    public string? Search { get; set; }
    public string Sort { get; set; } = "email";
}

// just the columns the list needs
public class AdminCustomerRowModel
{
    public string Id { get; set; } = "";
    public string? Email { get; set; }
    // name from their latest order (identity users don't have a name)
    public string? LatestOrderName { get; set; }
    public bool EmailConfirmed { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public bool IsLockedOut => LockoutEnd > DateTimeOffset.UtcNow;
    public int OrderCount { get; set; }
    public DateTime? LastOrderDate { get; set; }
    // paid orders that weren't cancelled/returned/refunded, same as the dashboard revenue
    public decimal TotalSpent { get; set; }
}

// everything on the customer details page
public class AdminCustomerDetailsModel
{
    public string Id { get; set; } = "";
    public string? Email { get; set; }
    public string? UserName { get; set; }
    public string? PhoneNumber { get; set; }
    public bool EmailConfirmed { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
    public bool IsLockedOut => LockoutEnd > DateTimeOffset.UtcNow;
    public int AccessFailedCount { get; set; }
    public int WishlistCount { get; set; }
    public int ReviewCount { get; set; }
    // address etc from the latest order, null if they never ordered
    public AdminCustomerDeliveryModel? LatestDelivery { get; set; }
    // all their orders except deleted ones, newest first
    public List<OrderSummaryModel> Orders { get; set; } = new();
    public decimal TotalSpent { get; set; }
}

public record AdminCustomerDeliveryModel(string? Name, string? MobileNumber, string? Address);
