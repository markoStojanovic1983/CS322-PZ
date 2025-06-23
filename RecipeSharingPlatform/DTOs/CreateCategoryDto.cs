using System.ComponentModel.DataAnnotations;

namespace RecipeSharingPlatform.DTOs
{
    public class CreateCategoryDto
    {
        [Required]
        [StringLength(100, ErrorMessage = "Category name must be between 1 and 100 characters.")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description must be 500 characters or less.")]
        public string Description { get; set; } = string.Empty;
    }
}
