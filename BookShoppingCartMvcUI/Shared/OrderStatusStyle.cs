namespace BookShoppingCartMvcUI.Shared;

// how order statuses look. colour is just extra, the name is always shown
public static class OrderStatusStyle
{
    public static string BadgeClass(string? status) => status switch
    {
        OrderWorkflow.Pending => "text-bg-warning",
        OrderWorkflow.Shipped => "text-bg-info",
        OrderWorkflow.Delivered => "text-bg-success",
        OrderWorkflow.Cancelled or OrderWorkflow.Returned or OrderWorkflow.Refund => "text-bg-secondary",
        _ => "text-bg-light border"
    };

    // short explanation of the status for the customer
    public static string Explain(string? status) => status switch
    {
        OrderWorkflow.Pending => "We've received your order and are getting it ready.",
        OrderWorkflow.Shipped => "Your books are on the way.",
        OrderWorkflow.Delivered => "Your books have been delivered. Enjoy!",
        OrderWorkflow.Cancelled => "This order was cancelled.",
        OrderWorkflow.Returned => "This order was returned.",
        OrderWorkflow.Refund => "This order has been refunded.",
        _ => "We'll update this as your order progresses."
    };

    // normal order steps, cancelled/returned/refund aren't on this track
    public static readonly IReadOnlyList<string> Track = [OrderWorkflow.Pending, OrderWorkflow.Shipped, OrderWorkflow.Delivered];

    // where the status is on the track, -1 if it isn't on it
    public static int TrackStep(string? status) => status is null ? -1 : Track.ToList().IndexOf(status);
}
