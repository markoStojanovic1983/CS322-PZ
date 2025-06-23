using System.ComponentModel.DataAnnotations;

namespace RecipeSharingPlatform.DTOs
{
    public class CreateRecipeDto
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(1, 1440)]
        public int PreparationTime { get; set; }

        [Required]
        [Range(1, 1440)]
        public int CookingTime { get; set; }

        [Required]
        [Range(1, 50)]
        public int Servings { get; set; }

        [Required]
        public int CategoryID { get; set; }

        public IFormFile? MainImage { get; set; }

        public List<CreateIngredientDto> Ingredients { get; set; } = new();
        public List<CreateStepDto> Steps { get; set; } = new();
    }

    public class UpdateRecipeDto
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [Range(1, 1440)]
        public int PreparationTime { get; set; }

        [Required]
        [Range(1, 1440)]
        public int CookingTime { get; set; }

        [Required]
        [Range(1, 50)]
        public int Servings { get; set; }

        [Required]
        public int CategoryID { get; set; }

        public IFormFile? MainImage { get; set; }
        public bool RemoveMainImage { get; set; } = false;

        public List<CreateIngredientDto> Ingredients { get; set; } = new();
        public List<UpdateStepDto> Steps { get; set; } = new();
    }

    public class CreateIngredientDto
    {
        [Required]
        [StringLength(100)]
        public string IngredientName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Quantity { get; set; } = string.Empty;

        [StringLength(20)]
        public string Unit { get; set; } = string.Empty;
    }

    public class CreateStepDto
    {
        [Required]
        [StringLength(1000)]
        public string StepDescription { get; set; } = string.Empty;

        public IFormFile? StepImage { get; set; }
    }

    public class UpdateStepDto
    {
        [Required]
        [StringLength(1000)]
        public string StepDescription { get; set; } = string.Empty;

        public IFormFile? StepImage { get; set; }
        public bool RemoveStepImage { get; set; } = false;
    }
}