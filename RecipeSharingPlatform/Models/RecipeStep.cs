using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RecipeSharingPlatform.Models
{
    public class RecipeStep
    {
        [Key]
        public int StepID { get; set; }

        [Required]
        public int RecipeID { get; set; }

        [Required]
        public int StepNumber { get; set; }

        [Required]
        [StringLength(1000)]
        public string StepDescription { get; set; } = string.Empty;

        public byte[]? StepImage { get; set; }

        // Navigation property
        [ForeignKey("RecipeID")]
        public virtual Recipe Recipe { get; set; } = null!;
    }
}