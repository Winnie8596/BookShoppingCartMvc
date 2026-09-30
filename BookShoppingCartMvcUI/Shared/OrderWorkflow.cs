namespace BookShoppingCartMvcUI.Shared;

// which status an order can move to next (the 6 seeded ones). orders only go forward,
// so delivered can't go back to pending and a cancelled one can't be shipped
public static class OrderWorkflow
{
    public const string Pending = "Pending";
    public const string Shipped = "Shipped";
    public const string Delivered = "Delivered";
    public const string Cancelled = "Cancelled";
    public const string Returned = "Returned";
    public const string Refund = "Refund";

    public static readonly string[] Statuses = { Pending, Shipped, Delivered, Cancelled, Returned, Refund };

    private static readonly Dictionary<string, string[]> Next = new()
    {
        // books leave when it ships, before that it can still be cancelled
        [Pending] = new[] { Shipped, Cancelled },
        // after shipping it's a return, not a cancel
        [Shipped] = new[] { Delivered, Returned },
        [Delivered] = new[] { Returned },
        [Returned] = new[] { Refund },
        // cancelled but paid orders still need a refund
        [Cancelled] = new[] { Refund },
        [Refund] = Array.Empty<string>()
    };

    // statuses it can move to from `from`, unknown status = none
    public static IReadOnlyList<string> NextStatuses(string? from) =>
        from is not null && Next.TryGetValue(from, out var next) ? next : Array.Empty<string>();

    public static bool CanMove(string? from, string? to) => to is not null && NextStatuses(from).Contains(to);

    // cancelling restocks since the books never left
    public static bool RestocksOnEntry(string? status) => status == Cancelled;
}
