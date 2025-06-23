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
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<EditModel> _logger;

        public EditModel(ApplicationDbContext context, UserManager<User> userManager, ILogger<EditModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModelEditRecipe Input { get; set; } = new InputModelEditRecipe();

        [BindProperty]
        public int RecipeID { get; set; }

        [BindProperty]
        public bool RemoveMainImage { get; set; } = false;

        public SelectList Categories { get; set; }
        public bool HasCurrentMainImage { get; set; }


        public async Task<IActionResult> OnGetAsync(int id)
        {
            await LoadCategoriesAsync();

            var recipe = await _context.Recipes
                .Include(r => r.Ingredients)
                .Include(r => r.RecipeSteps.OrderBy(s => s.StepNumber))
                .FirstOrDefaultAsync(r => r.RecipeID == id);

            if (recipe == null)
                return NotFound();

            // Check if user owns this recipe
            var user = await _userManager.GetUserAsync(User);
            if (recipe.UserID != user.Id)
                return Forbid();

            // Populate form with existing data
            RecipeID = recipe.RecipeID;
            HasCurrentMainImage = recipe.MainImage != null && recipe.MainImage.Length > 0;

            Input.Title = recipe.Title;
            Input.Description = recipe.Description;
            Input.PreparationTime = recipe.PreparationTime;
            Input.CookingTime = recipe.CookingTime;
            Input.Servings = recipe.Servings;
            Input.CategoryID = recipe.CategoryID;

            // Load existing ingredients
            foreach (var ingredient in recipe.Ingredients)
            {
                Input.Ingredients.Add(new IngredientInput
                {
                    IngredientName = ingredient.IngredientName,
                    Quantity = ingredient.Quantity,
                    Unit = ingredient.Unit
                });
            }

            // Load existing steps
            foreach (var step in recipe.RecipeSteps)
            {
                Input.RecipeSteps.Add(new RecipeStepEditInput
                {
                    StepDescription = step.StepDescription,
                    HasCurrentImage = step.StepImage != null && step.StepImage.Length > 0,
                    StepID = step.StepID
                });
            }

            // Ensure at least one ingredient and step for the form
            if (!Input.Ingredients.Any())
                Input.Ingredients.Add(new IngredientInput());

            if (!Input.RecipeSteps.Any())
                Input.RecipeSteps.Add(new RecipeStepEditInput());

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await LoadCategoriesAsync();

            if (!ModelState.IsValid)
                return Page();

            // Image validation
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
                var recipe = await _context.Recipes
                    .Include(r => r.Ingredients)
                    .Include(r => r.RecipeSteps)
                    .FirstOrDefaultAsync(r => r.RecipeID == RecipeID);

                if (recipe == null)
                    return NotFound();

                // Check if user owns this recipe
                var user = await _userManager.GetUserAsync(User);
                if (recipe.UserID != user.Id)
                    return Forbid();

                // Update basic recipe properties
                recipe.Title = Input.Title;
                recipe.Description = Input.Description;
                recipe.PreparationTime = Input.PreparationTime;
                recipe.CookingTime = Input.CookingTime;
                recipe.Servings = Input.Servings;
                recipe.CategoryID = Input.CategoryID;
                recipe.ModifiedDate = DateTime.UtcNow;

                // Handle main image
                if (RemoveMainImage)
                {
                    recipe.MainImage = null;
                }
                else if (Input.MainImageFile != null)
                {
                    recipe.MainImage = await ImageController.ToByteArrayAsync(Input.MainImageFile);
                }

                // Remove existing ingredients and steps
                _context.Ingredients.RemoveRange(recipe.Ingredients);
                _context.RecipeSteps.RemoveRange(recipe.RecipeSteps);

                // Add new ingredients
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

                // Add new steps
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

                        // Handle step image - only add new image if provided
                        if (stepInput.StepImageFile != null)
                        {
                            step.StepImage = await ImageController.ToByteArrayAsync(stepInput.StepImageFile);
                        }
                        // Note: If no new image and RemoveImage is true, step.StepImage stays null

                        _context.RecipeSteps.Add(step);
                    }
                }

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Recipe updated successfully!";
                return RedirectToPage("/Profile/MyRecipes");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating recipe {RecipeId} for user {UserId}", RecipeID, User.Identity?.Name);
                ModelState.AddModelError("", "An error occurred while updating the recipe. Please try again.");
                return Page();
            }
        }

        // Image validation method
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

        // Simple validation for ingredients and steps
        private string ValidateIngredientsAndSteps()
        {
            bool hasIngredient = Input.Ingredients.Any(i => !string.IsNullOrWhiteSpace(i.IngredientName));
            if (!hasIngredient)
                return "At least one ingredient is required";

            bool hasStep = Input.RecipeSteps.Any(s => !string.IsNullOrWhiteSpace(s.StepDescription));
            if (!hasStep)
                return "At least one step is required";

            return null;
        }

        // Helper method to load categories for the dropdown
        private async Task LoadCategoriesAsync()
        {
            var categories = await _context.Categories.ToListAsync();
            Categories = new SelectList(categories, "CategoryID", "CategoryName");
        }
    }
}