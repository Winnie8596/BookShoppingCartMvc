using System.ComponentModel.DataAnnotations;

namespace BookShoppingCartMvcUI.Models.DTOs
{
    public class GenreDTO
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Please enter the genre name.")]
        [MaxLength(40, ErrorMessage = "Please keep the genre name to 40 characters or fewer.")]
        [Display(Name = "Genre")]
        public string GenreName { get; set; }
    }
}
