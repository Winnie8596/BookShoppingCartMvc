using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookShoppingCartMvcUI.Models
{
    // book saved for later. one row per user per book (unique index)
    [Table("WishlistItem")]
    public class WishlistItem
    {
        public int Id { get; set; }
        [Required]
        public string UserId { get; set; } = string.Empty;
        [Required]
        public int BookId { get; set; }
        public DateTime CreatedAt { get; set; }
        public Book Book { get; set; }
    }
}
