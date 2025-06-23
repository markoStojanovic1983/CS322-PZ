using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RecipeSharingPlatform.Data;
using RecipeSharingPlatform.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace RecipeSharingPlatform.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class PendingRecipesModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PendingRecipesModel> _logger;

        public PendingRecipesModel(ApplicationDbContext context, ILogger<PendingRecipesModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public List<Recipe> PendingRecipes { get; set; } = new();

        [BindProperty]
        public ModerationAction Action { get; set; } = new();


        public async Task OnGetAsync()
        {
            await LoadPendingRecipesAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadPendingRecipesAsync();
                return Page();
            }

            try
            {
                bool success = false;

                if (Action.ActionType == "approve")
                {
                    success = await ApproveRecipeAsync(Action.RecipeId, Action.Notes);
                    if (success)
                    {
                        TempData["SuccessMessage"] = "Recipe approved successfully!";
                    }
                }
                else if (Action.ActionType == "reject")
                {
                    if (string.IsNullOrWhiteSpace(Action.Notes))
                    {
                        ModelState.AddModelError("Action.Notes", "Rejection reason is required.");
                        await LoadPendingRecipesAsync();
                        return Page();
                    }

                    success = await RejectRecipeAsync(Action.RecipeId, Action.Notes);
                    if (success)
                    {
                        TempData["SuccessMessage"] = "Recipe rejected with feedback provided to chef.";
                    }
                }

                if (!success)
                {
                    TempData["ErrorMessage"] = "An error occurred while processing the recipe.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing recipe moderation action");
                TempData["ErrorMessage"] = "An error occurred while processing the recipe.";
            }

            // Clear the form and reload
            Action = new ModerationAction();
            await LoadPendingRecipesAsync();
            return Page();
        }

        // Handle individual recipe approval
        public async Task<IActionResult> OnPostApproveAsync(int recipeId, string notes = "")
        {
            try
            {
                var success = await ApproveRecipeAsync(recipeId, notes);
                if (success)
                {
                    TempData["SuccessMessage"] = "Recipe approved successfully!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to approve recipe.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving recipe with ID {RecipeId}", recipeId);
                TempData["ErrorMessage"] = "An error occurred while approving the recipe.";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRejectAsync(int recipeId, string notes)
        {
            if (string.IsNullOrWhiteSpace(notes))
            {
                TempData["ErrorMessage"] = "Rejection reason is required.";
                return RedirectToPage();
            }

            try
            {
                var success = await RejectRecipeAsync(recipeId, notes);
                if (success)
                {
                    TempData["SuccessMessage"] = "Recipe rejected with feedback provided to chef.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to reject recipe.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting recipe with ID {RecipeId}", recipeId);
                TempData["ErrorMessage"] = "An error occurred while rejecting the recipe.";
            }

            return RedirectToPage();
        }

        // Helper method to load pending recipes
        private async Task LoadPendingRecipesAsync()
        {
            PendingRecipes = await _context.Recipes
                .Include(r => r.User)
                .Include(r => r.Category)
                .Include(r => r.Ingredients)
                .Include(r => r.RecipeSteps.OrderBy(s => s.StepNumber))
                .Where(r => !r.IsApproved && !r.IsRejected)
                .OrderBy(r => r.CreatedDate) // Oldest first for fair review
                .ToListAsync();
        }

        // Helper method to approve recipe
        private async Task<bool> ApproveRecipeAsync(int id, string moderationNotes = "")
        {
            try
            {
                var recipe = await _context.Recipes.FindAsync(id);
                if (recipe == null) return false;

                recipe.IsApproved = true;
                recipe.IsRejected = false;
                recipe.ModerationNotes = moderationNotes;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving recipe with ID {RecipeId}", id);
                return false;
            }
        }

        // Helper method to reject recipe
        private async Task<bool> RejectRecipeAsync(int id, string moderationNotes)
        {
            try
            {
                var recipe = await _context.Recipes.FindAsync(id);
                if (recipe == null) return false;

                recipe.IsApproved = false;
                recipe.IsRejected = true;
                recipe.ModerationNotes = moderationNotes ?? "Recipe rejected";

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting recipe with ID {RecipeId}", id);
                return false;
            }
        }
    }
}