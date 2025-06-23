using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RecipeSharingPlatform.Models
{
    public class Recipe
    {
        [Key]
        public int RecipeID { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        public int PreparationTime { get; set; } // in minutes

        public int CookingTime { get; set; } // in minutes

        public int Servings { get; set; }

        public byte[]? MainImage { get; set; }

        // Foreign Keys
        [Required]
        public string UserID { get; set; } = string.Empty;

        [Required]
        public int CategoryID { get; set; }

        // Moderation fields
        public bool IsApproved { get; set; } = false;
        public bool IsRejected { get; set; } = false;

        [StringLength(500)]
        public string? ModerationNotes { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("UserID")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("CategoryID")]
        public virtual Category Category { get; set; } = null!;

        public virtual ICollection<Ingredient> Ingredients { get; set; } = new List<Ingredient>();
        public virtual ICollection<RecipeStep> RecipeSteps { get; set; } = new List<RecipeStep>();
        public virtual ICollection<UserFavorite> Favorites { get; set; } = new List<UserFavorite>();
        public virtual ICollection<Rating> Ratings { get; set; } = new List<Rating>();
    }
}