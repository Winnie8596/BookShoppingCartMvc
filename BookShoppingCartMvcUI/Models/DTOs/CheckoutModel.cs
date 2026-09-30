using System.ComponentModel.DataAnnotations;

namespace BookShoppingCartMvcUI.Models.DTOs;

// these show up next to the fields, so they say what to do instead of what went wrong
public class CheckoutModel : IValidatableObject
{
    [Display(Name = "Full name")]
    [Required(ErrorMessage = "Please enter your name.")]
    [MaxLength(30, ErrorMessage = "Please keep your name to 30 characters or fewer.")]
    public string? Name { get; set; }

    [Display(Name = "Email")]
    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address, like name@example.com.")]
    [MaxLength(30, ErrorMessage = "Please use an email address of 30 characters or fewer.")]
    public string? Email { get; set; }

    [Display(Name = "Mobile number")]
    [Required(ErrorMessage = "Please enter a mobile number so the courier can reach you.")]
    [Phone(ErrorMessage = "Please enter a valid phone number, for example 0123456789.")]
    [MaxLength(20, ErrorMessage = "Please keep the mobile number to 20 characters or fewer.")]
    public string? MobileNumber { get; set; }

    [Display(Name = "Delivery address")]
    [Required(ErrorMessage = "Please enter the address we should deliver to.")]
    [MaxLength(200, ErrorMessage = "Please keep the address to 200 characters or fewer.")]
    public string? Address { get; set; }

    [Display(Name = "Payment method")]
    [Required(ErrorMessage = "Please choose how you'd like to pay.")]
    public string? PaymentMethod { get; set; }

    // payment methods we accept. not using Enum.TryParse since it also accepts numbers like "7"
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (PaymentMethod is not null && !Enum.GetNames<PaymentMethods>().Contains(PaymentMethod))
            yield return new ValidationResult("Please choose a valid payment method.", new[] { nameof(PaymentMethod) });
    }
}
