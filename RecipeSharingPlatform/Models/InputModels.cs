using System.ComponentModel.DataAnnotations;

namespace RecipeSharingPlatform.Models
{

    public class CategoryInput
    {
        [Required]
        [StringLength(100, ErrorMessage = "Category name must be between 1 and 100 characters.")]
        public string CategoryName { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Description must be 500 characters or less.")]
        public string Description { get; set; } = string.Empty;
    }

    public class InputModelProfile
    {
        [Required]
        [StringLength(50, ErrorMessage = "First name must be between 1 and 50 characters.")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50, ErrorMessage = "Last name must be between 1 and 50 characters.")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [StringLength(50, ErrorMessage = "Username must be between 1 and 50 characters.")]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Profile Image")]
        public IFormFile? ProfileImageFile { get; set; } // Made nullable with ?
    }


    public class InputModelRecipe
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        public int PreparationTime { get; set; }
        public int CookingTime { get; set; }
        public int Servings { get; set; }

        [Required]
        public int CategoryID { get; set; }

        public IFormFile? MainImageFile { get; set; }

        public List<IngredientInput> Ingredients { get; set; } = new List<IngredientInput>();
        public List<RecipeStepInput> RecipeSteps { get; set; } = new List<RecipeStepInput>();
    }

    public class InputModelEditRecipe
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string Description { get; set; } = string.Empty;

        public int PreparationTime { get; set; }
        public int CookingTime { get; set; }
        public int Servings { get; set; }

        [Required]
        public int CategoryID { get; set; }

        public IFormFile? MainImageFile { get; set; }

        public List<IngredientInput> Ingredients { get; set; } = new List<IngredientInput>();
        public List<RecipeStepEditInput> RecipeSteps { get; set; } = new List<RecipeStepEditInput>();
    }

    public class IngredientInput
    {
        public string IngredientName { get; set; } = string.Empty;
        public string Quantity { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
    }

    public class RecipeStepInput
    {
        public string StepDescription { get; set; } = string.Empty;
        public IFormFile? StepImageFile { get; set; }
    }

    public class RecipeStepEditInput
    {
        public string StepDescription { get; set; } = string.Empty;
        public IFormFile? StepImageFile { get; set; } 
        public bool HasCurrentImage { get; set; } = false;
        public bool RemoveImage { get; set; } = false;
        public int StepID { get; set; } = 0; 
    }

    public class InputModelChangePassword
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm New Password")]
        [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class RatingInput
    {
        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5 stars.")]
        public int Rating { get; set; }

        [StringLength(500, ErrorMessage = "Comment must be 500 characters or less.")]
        public string Comment { get; set; } = string.Empty;
    }
}