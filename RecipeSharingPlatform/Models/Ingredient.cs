using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RecipeSharingPlatform.Models
{
    public class Ingredient
    {
        [Key]
        public int IngredientID { get; set; }

        [Required]
        public int RecipeID { get; set; }

        [Required]
        [StringLength(100)]
        public string IngredientName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Quantity { get; set; } = string.Empty;

        [StringLength(20)]
        public string Unit { get; set; } = string.Empty;

        // Navigation property
        [ForeignKey("RecipeID")]
        public virtual Recipe Recipe { get; set; } = null!;
    }
}