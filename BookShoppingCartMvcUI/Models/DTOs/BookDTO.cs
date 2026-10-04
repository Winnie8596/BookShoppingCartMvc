using BookShoppingCartMvcUI.Shared;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace BookShoppingCartMvcUI.Models.DTOs;
public class BookDTO
{
    public const double MaxPrice = 100_000;

    public int Id { get; set; }

    [Required(ErrorMessage = "Please enter the title.")]
    [MaxLength(40, ErrorMessage = "Please keep the title to 40 characters or fewer.")]
    [Display(Name = "Title")]
    public string? BookName { get; set; }

    [Required(ErrorMessage = "Please enter the author.")]
    [MaxLength(40, ErrorMessage = "Please keep the author to 40 characters or fewer.")]
    [Display(Name = "Author")]
    public string? AuthorName { get; set; }

    [Range(0.01, MaxPrice, ErrorMessage = "The price must be between RM0.01 and RM100,000.")]
    public decimal Price { get; set; }

    // just for showing the current cover, the saved value always comes from the db
    public string? Image { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Please choose a genre.")]
    [Display(Name = "Genre")]
    public int GenreId { get; set; }

    // only used when adding, after that stock is changed on the Inventory page
    [Range(0, StockLevel.MaxQuantity, ErrorMessage = "Stock must be between 0 and 100,000.")]
    [Display(Name = "Initial stock")]
    public int InitialStock { get; set; }

    [Display(Name = "Cover image")]
    public IFormFile? ImageFile { get; set; }
    public IEnumerable<SelectListItem>? GenreList { get; set; }
}
