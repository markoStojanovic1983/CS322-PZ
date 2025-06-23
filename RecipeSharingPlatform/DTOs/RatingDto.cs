using System.ComponentModel.DataAnnotations;

namespace RecipeSharingPlatform.DTOs
{
    public class RatingDto
    {
        public int RatingId { get; set; }
        public int RecipeId { get; set; }
        public string RecipeTitle { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public int Score { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime RatingDate { get; set; }
    }

    public class CreateRatingDto
    {
        [Required]
        public int RecipeId { get; set; }

        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int Score { get; set; }

        [StringLength(500, ErrorMessage = "Comment must be 500 characters or less")]
        public string Comment { get; set; } = string.Empty;
    }

    public class UpdateRatingDto
    {
        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int Score { get; set; }

        [StringLength(500, ErrorMessage = "Comment must be 500 characters or less")]
        public string Comment { get; set; } = string.Empty;
    }
}