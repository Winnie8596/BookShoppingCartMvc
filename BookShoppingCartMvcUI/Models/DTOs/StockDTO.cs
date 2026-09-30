using BookShoppingCartMvcUI.Shared;
using System.ComponentModel.DataAnnotations;

namespace BookShoppingCartMvcUI.Models.DTOs
{
    // set stock to an exact number (e.g. after a stocktake)
    public class StockDTO
    {
        public int BookId { get; set; }

        [Range(0, StockLevel.MaxQuantity, ErrorMessage = "Quantity must be between 0 and 100,000.")]
        public int Quantity { get; set; }

        // the count the admin saw when opening the form. only saves if the stock is still that,
        // so we don't overwrite a sale that happened in between. null = no check
        public int? ExpectedQuantity { get; set; }
    }

    // add (delivery) or remove (damaged/lost) copies from the current stock
    public class StockAdjustmentDTO
    {
        public int BookId { get; set; }

        [Required(ErrorMessage = "Enter how many copies to add or remove.")]
        [Range(-StockLevel.MaxQuantity, StockLevel.MaxQuantity, ErrorMessage = "The change must be between -100,000 and 100,000.")]
        public int? Change { get; set; }
    }

    // Manage stock page. field names match both form DTOs so either post can redisplay it
    public class StockManageModel
    {
        public int BookId { get; set; }
        public string? BookName { get; set; }
        public string? AuthorName { get; set; }
        // from the db, just for display
        public int CurrentQuantity { get; set; }

        [Display(Name = "New stock count")]
        public int Quantity { get; set; }
        public int? ExpectedQuantity { get; set; }

        [Display(Name = "Copies to add or remove")]
        public int? Change { get; set; }
    }

    public enum StockChangeStatus
    {
        Updated,
        BookNotFound,
        InvalidQuantity,
        // stock changed since the admin opened the page
        StockChanged,
        BelowZero,
        AboveMaximum
    }

    // result of a stock change + the new count
    public record StockChangeResult(StockChangeStatus Status, int Quantity)
    {
        public bool Succeeded => Status == StockChangeStatus.Updated;
    }
}
