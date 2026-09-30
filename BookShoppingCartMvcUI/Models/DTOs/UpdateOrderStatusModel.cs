using System.ComponentModel.DataAnnotations;

namespace BookShoppingCartMvcUI.Models.DTOs;

public class UpdateOrderStatusModel
{
    public int OrderId { get; set; }

    [Required(ErrorMessage = "Choose a new status.")]
    public int? OrderStatusId { get; set; }

    // status when the admin loaded the page. if set, only update if it's still that
    public int? ExpectedStatusId { get; set; }
}
