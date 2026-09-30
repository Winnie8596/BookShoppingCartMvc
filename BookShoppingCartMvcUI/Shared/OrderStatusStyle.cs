namespace BookShoppingCartMvcUI.Shared;

// how order statuses look. colour is just extra, the name is always shown
public static class OrderStatusStyle
{
    public static string BadgeClass(string? status) => status switch
    {
        "Pending" => "text-bg-warning",
        "Shipped" => "text-bg-info",
        "Delivered" => "text-bg-success",
        "Cancelled" or "Returned" or "Refund" => "text-bg-secondary",
        _ => "text-bg-light border"
    };

    // short explanation of the status for the customer
    public static string Explain(string? status) => status switch
    {
        "Pending" => "We've received your order and are getting it ready.",
        "Shipped" => "Your books are on the way.",
        "Delivered" => "Your books have been delivered. Enjoy!",
        "Cancelled" => "This order was cancelled.",
        "Returned" => "This order was returned.",
        "Refund" => "This order has been refunded.",
        _ => "We'll update this as your order progresses."
    };

    // normal order steps, cancelled/returned/refund aren't on this track
    public static readonly string[] Track = { "Pending", "Shipped", "Delivered" };
}
