using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RecipeSharingPlatform.Models
{
    public class Rating
    {
        [Key]
        public int RatingID { get; set; }

        [Required]
        public int RecipeID { get; set; }

        [Required]
        public string UserID { get; set; } = string.Empty;

        [Required]
        [Range(1, 5)]
        public int Score { get; set; }

        [StringLength(500)]
        public string? Comment { get; set; }

        public DateTime RatingDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("RecipeID")]
        public virtual Recipe Recipe { get; set; } = null!;

        [ForeignKey("UserID")]
        public virtual User User { get; set; } = null!;
    }
}