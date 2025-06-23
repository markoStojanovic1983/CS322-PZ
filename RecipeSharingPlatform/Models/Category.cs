using System.ComponentModel.DataAnnotations;

namespace RecipeSharingPlatform.Models
{
    public class Category
    {
        [Key]
        public int CategoryID { get; set; }

        [Required]
        [StringLength(100)]
        public string CategoryName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        // Navigation property
        public virtual ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();
    }
}