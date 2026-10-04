using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookShoppingCartMvcUI.Models;

[Table("Book")]
public class Book
{
    public int Id { get; set; }

    [Required]
    [MaxLength(40)]
    public string BookName { get; set; } = string.Empty;

    [Required]
    [MaxLength(40)]
    public string AuthorName { get; set; } = string.Empty;
    [Required]
    public decimal Price { get; set; }
    public string? Image { get; set; }
    [Required]
    public int GenreId { get; set; }
    public Genre Genre { get; set; } = null!;
    public List<OrderDetail> OrderDetail { get; set; } = new();
    public List<CartDetail> CartDetail { get; set; } = new();
    public Stock? Stock { get; set; }
    public List<Review> Reviews { get; set; } = new();
}
