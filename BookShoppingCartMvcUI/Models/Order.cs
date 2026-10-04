using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookShoppingCartMvcUI.Models;

[Table("Order")]
public class Order
{
    public int Id { get; set; }
    [Required]
    public string UserId { get; set; } = string.Empty;
    public DateTime CreateDate { get; set; } = DateTime.UtcNow;
    [Required]
    public int OrderStatusId { get; set; }
    // soft delete, there's a query filter for it in ApplicationDbContext
    public bool IsDeleted { get; set; } = false;
    [Required]
    [MaxLength(30)]
    public string Name { get; set; } = string.Empty;

    // 256 = same as AspNetUsers.Email
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public string Email { get; set; } = string.Empty;
    [Required]
    [MaxLength(20)]
    public string MobileNumber { get; set; } = string.Empty;
    [Required]
    [MaxLength(200)]
    public string Address { get; set; } = string.Empty;
    [Required]
    [MaxLength(30)]
    public string PaymentMethod { get; set; } = string.Empty;
    public bool IsPaid { get; set; }

    public OrderStatus OrderStatus { get; set; } = null!;
    public List<OrderDetail> OrderDetail { get; set; } = new();
}
