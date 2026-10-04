using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BookShoppingCartMvcUI.Models;

// rating + review. one per user per book (unique index on UserId + BookId)
[Table("Review")]
public class Review
{
    public const int MinRating = 1;
    public const int MaxRating = 5;
    public const int TitleMaxLength = 100;
    public const int CommentMinLength = 10;
    public const int CommentMaxLength = 1000;

    public int Id { get; set; }
    [Required]
    public string UserId { get; set; } = string.Empty;
    [Required]
    public int BookId { get; set; }
    [Range(MinRating, MaxRating)]
    public int Rating { get; set; }
    [MaxLength(TitleMaxLength)]
    public string? Title { get; set; }
    [Required]
    [MaxLength(CommentMaxLength)]
    public string Comment { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    // null until it's edited
    public DateTime? UpdatedAt { get; set; }
    public Book Book { get; set; } = null!;
}
