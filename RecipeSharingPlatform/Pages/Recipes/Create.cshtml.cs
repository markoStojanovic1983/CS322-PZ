using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using RecipeSharingPlatform.Data;
using RecipeSharingPlatform.Models;
using RecipeSharingPlatform.Controllers;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace RecipeSharingPlatform.Pages.Recipes
{
    [Authorize(Roles = "Chef")]
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(ApplicationDbContext context, UserManager<User> userManager, ILogger<CreateModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModelRecipe Input { get; set; } = new InputModelRecipe();

        public SelectList Categories { get; set; }
 
        public async Task<IActionResult> OnGetAsync()
        {
            await LoadCategoriesAsync();

            // Initialize with one ingredient and one step for the form
            Input.Ingredients.Add(new IngredientInput());
            Input.RecipeSteps.Add(new RecipeStepInput());

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Reload categories for dropdown in case of errors
            await LoadCategoriesAsync();

            if (!ModelState.IsValid)
                return Page();

            // Validate main image and step images
            var imageError = ValidateImages();
            if (imageError != null)
            {
                ModelState.AddModelError("", imageError);
                return Page();
            }

            // Validate ingredients and steps
            var validationError = ValidateIngredientsAndSteps();
            if (validationError != null)
            {
                ModelState.AddModelError("", validationError);
                return Page();
            }

            try
            {
                // Get current user
                var user = await _userManager.GetUserAsync(User);

                // Create recipe from input
                var recipe = new Recipe
                {
                    Title = Input.Title,
                    Description = Input.Description,
                    PreparationTime = Input.PreparationTime,
                    CookingTime = Input.CookingTime,
                    Servings = Input.Servings,
                    CategoryID = Input.CategoryID,
                    UserID = user.Id,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow,
                    IsApproved = false,
                    IsRejected = false
                };

                // Handle main image with simple validation
                if (Input.MainImageFile != null)
                    recipe.MainImage = await ImageController.ToByteArrayAsync(Input.MainImageFile);

                _context.Recipes.Add(recipe);
                await _context.SaveChangesAsync(); // Save to get recipe ID

                // Add ingredients
                for (int i = 0; i < Input.Ingredients.Count; i++)
                {
                    var ingredientInput = Input.Ingredients[i];
                    if (!string.IsNullOrWhiteSpace(ingredientInput.IngredientName))
                    {
                        var ingredient = new Ingredient
                        {
                            RecipeID = recipe.RecipeID,
                            IngredientName = ingredientInput.IngredientName.Trim(),
                            Quantity = ingredientInput.Quantity?.Trim() ?? "",
                            Unit = ingredientInput.Unit?.Trim() ?? ""
                        };
                        _context.Ingredients.Add(ingredient);
                    }
                }

                // Add steps
                for (int i = 0; i < Input.RecipeSteps.Count; i++)
                {
                    var stepInput = Input.RecipeSteps[i];
                    if (!string.IsNullOrWhiteSpace(stepInput.StepDescription))
                    {
                        var step = new RecipeStep
                        {
                            RecipeID = recipe.RecipeID,
                            StepNumber = i + 1,
                            StepDescription = stepInput.StepDescription.Trim()
                        };

                        // Handle step image with simple validation
                        if (stepInput.StepImageFile != null)
                            step.StepImage = await ImageController.ToByteArrayAsync(stepInput.StepImageFile);

                        _context.RecipeSteps.Add(step);
                    }
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Recipe created successfully! It will be reviewed by an administrator before being published.";
                return RedirectToPage("/Profile/MyRecipes");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating recipe for user {UserId}", User.Identity?.Name);
                ModelState.AddModelError("", "An error occurred while creating the recipe. Please try again.");
                return Page();
            }
        }

        // Helper method to validate images
        private string ValidateImages()
        {
            // Check main image
            if (Input.MainImageFile != null)
            {
                var mainImageError = ImageController.ValidateImage(Input.MainImageFile, "Main image");
                if (mainImageError != null)
                    return mainImageError;
            }

            // Check step images
            for (int i = 0; i < Input.RecipeSteps.Count; i++)
            {
                if (Input.RecipeSteps[i].StepImageFile != null)
                {
                    var stepImageError = ImageController.ValidateImage(Input.RecipeSteps[i].StepImageFile, $"Step {i + 1} image");
                    if (stepImageError != null)
                        return stepImageError;
                }
            }

            return null; // No errors
        }

        // Helper method to validate ingredients and steps
        private string ValidateIngredientsAndSteps()
        {
            // Check if at least one ingredient is provided
            bool hasIngredient = Input.Ingredients.Any(i => !string.IsNullOrWhiteSpace(i.IngredientName));
            if (!hasIngredient)
                return "At least one ingredient is required";

            // Check if at least one step is provided
            bool hasStep = Input.RecipeSteps.Any(s => !string.IsNullOrWhiteSpace(s.StepDescription));
            if (!hasStep)
                return "At least one step is required";

            return null; // No errors
        }

        // Helper method to load categories for dropdown
        private async Task LoadCategoriesAsync()
        {
            var categories = await _context.Categories.ToListAsync();
            Categories = new SelectList(categories, "CategoryID", "CategoryName");
        }
    }
}