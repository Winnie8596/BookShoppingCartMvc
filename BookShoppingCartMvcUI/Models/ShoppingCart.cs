using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookShoppingCartMvcUI.Models;

[Table("ShoppingCart")]
public class ShoppingCart
{
    public int Id { get; set; }
    [Required]
    public string UserId { get; set; } = string.Empty;

    public ICollection<CartDetail> CartDetails { get; set; } = new List<CartDetail>();
}
