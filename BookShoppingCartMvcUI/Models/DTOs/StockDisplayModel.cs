namespace BookShoppingCartMvcUI.Models.DTOs
{
    public class StockDisplayModel
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        // 0 if there's no Stock row
        public int Quantity { get; set; }
        public string? BookName { get; set; }
        public string? AuthorName { get; set; }
    }
}
