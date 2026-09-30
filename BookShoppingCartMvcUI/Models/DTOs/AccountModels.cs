namespace BookShoppingCartMvcUI.Models.DTOs
{
    // row in My Orders. total uses the price they paid, not the current price
    public record OrderSummaryModel(int Id, DateTime CreateDate, string? StatusName, string? PaymentMethod,
        bool IsPaid, int ItemCount, double Total);

    // one book in an order (unit price from when they ordered)
    public record OrderLineModel(int BookId, string? BookName, string? AuthorName, string? Image, int Quantity, double UnitPrice)
    {
        public double Subtotal => UnitPrice * Quantity;
    }

    // an order as the customer sees it
    public class OrderDetailsModel
    {
        public int Id { get; set; }
        public DateTime CreateDate { get; set; }
        public string? StatusName { get; set; }
        public string? PaymentMethod { get; set; }
        public bool IsPaid { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public string? MobileNumber { get; set; }
        public string? Address { get; set; }
        public List<OrderLineModel> Lines { get; set; } = new();
        public double Total => Lines.Sum(l => l.Subtotal);
    }

    // one of the user's reviews, for My Reviews
    public record MyReviewModel(int BookId, string? BookName, string? Image, int Rating, string? Title, string Comment,
        DateTime CreatedAt, DateTime? UpdatedAt);
}
