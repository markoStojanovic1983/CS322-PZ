using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RecipeSharingPlatform.Models;
using RecipeSharingPlatform.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace RecipeSharingPlatform.Pages.Recipes
{
    public class DetailsModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DetailsModel> _logger;

        public DetailsModel(ApplicationDbContext context, ILogger<DetailsModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public Recipe Recipe { get; set; } = null!;
        public bool CanUserEdit { get; set; } = false;

        public bool IsUserFavorite { get; set; } = false;
        public int? UserRating { get; set; }
        public double AverageRating { get; set; }
        public int TotalRatings { get; set; }
        public int TotalFavorites { get; set; }
        public List<RecipeRating> RecentRatings { get; set; } = new();

        [BindProperty]
        public RatingInput Input { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            // Get recipe with all related data
            Recipe = await _context.Recipes
                .Include(r => r.User)
                .Include(r => r.Category)
                .Include(r => r.Ingredients)
                .Include(r => r.RecipeSteps.OrderBy(s => s.StepNumber))
                .Include(r => r.Ratings)
                .FirstOrDefaultAsync(r => r.RecipeID == id.Value);

            if (Recipe == null)
            {
                return NotFound();
            }

            // Check if recipe is approved (unless user is admin or recipe owner)
            if (!Recipe.IsApproved && !User.IsInRole("Admin"))
            {
                // Allow recipe owner to view their own pending recipes
                if (!User.Identity.IsAuthenticated || Recipe.UserID != User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value)
                {
                    return NotFound("This recipe is not yet approved.");
                }
            }

            // Check if current user can edit this recipe
            if (User.Identity.IsAuthenticated)
            {
                var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                CanUserEdit = Recipe.UserID == currentUserId;

                // Check if recipe is in user's favorites
                if (!string.IsNullOrEmpty(currentUserId))
                {
                    IsUserFavorite = await _context.UserFavorites
                        .AnyAsync(f => f.UserID == currentUserId && f.RecipeID == Recipe.RecipeID);

                    // Get user's existing rating
                    var userRating = await _context.Ratings
                        .FirstOrDefaultAsync(r => r.UserID == currentUserId && r.RecipeID == Recipe.RecipeID);
                    if (userRating != null)
                    {
                        UserRating = userRating.Score;
                        Input.Rating = userRating.Score;
                        Input.Comment = userRating.Comment;
                    }
                }
            }

            // Calculate ratings statistics
            if (Recipe.Ratings.Any())
            {
                AverageRating = Recipe.Ratings.Average(r => r.Score);
                TotalRatings = Recipe.Ratings.Count;

                // Get recent ratings for display
                RecentRatings = await _context.Ratings
                    .Include(r => r.User)
                    .Where(r => r.RecipeID == Recipe.RecipeID)
                    .OrderByDescending(r => r.RatingDate)
                    .Take(5)
                    .Select(r => new RecipeRating
                    {
                        UserName = $"{r.User.FirstName} {r.User.LastName}",
                        Score = r.Score,
                        Comment = r.Comment,
                        RatingDate = r.RatingDate
                    })
                    .ToListAsync();
            }

            // Get favorites count
            TotalFavorites = await _context.UserFavorites
                .CountAsync(f => f.RecipeID == Recipe.RecipeID);

            return Page();
        }

        // Toggle favorite status - ADD THIS METHOD
        public async Task<IActionResult> OnPostToggleFavoriteAsync(int id)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToPage("/Account/Login");
            }

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Account/Login");
            }

            try
            {
                // Check if recipe exists and is approved
                var recipe = await _context.Recipes
                    .FirstOrDefaultAsync(r => r.RecipeID == id && r.IsApproved);

                if (recipe == null)
                {
                    return NotFound("Recipe not found or not approved");
                }

                // Check if user owns this recipe
                if (recipe.UserID == userId)
                {
                    TempData["ErrorMessage"] = "You cannot add your own recipe to favorites.";
                    return RedirectToPage(new { id = id });
                }

                // Check if already in favorites
                var existingFavorite = await _context.UserFavorites
                    .FirstOrDefaultAsync(f => f.UserID == userId && f.RecipeID == id);

                if (existingFavorite != null)
                {
                    // Remove from favorites
                    _context.UserFavorites.Remove(existingFavorite);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Recipe removed from favorites.";
                }
                else
                {
                    // Add to favorites
                    var favorite = new UserFavorite
                    {
                        UserID = userId,
                        RecipeID = id,
                        DateAdded = DateTime.UtcNow
                    };

                    _context.UserFavorites.Add(favorite);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Recipe added to favorites!";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling favorite status for recipe {RecipeId} by user {UserId}", id, userId);
                TempData["ErrorMessage"] = "An error occurred while updating favorites.";
            }

            return RedirectToPage(new { id = id });
        }

        // Delete recipe - ADD THIS METHOD TOO
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToPage("/Account/Login");
            }

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Account/Login");
            }

            try
            {
                var recipe = await _context.Recipes
                    .Include(r => r.Ingredients)
                    .Include(r => r.RecipeSteps)
                    .Include(r => r.Ratings)
                    .Include(r => r.Favorites)
                    .FirstOrDefaultAsync(r => r.RecipeID == id && r.UserID == userId);

                if (recipe == null)
                {
                    return NotFound("Recipe not found or you don't have permission to delete it.");
                }

                // Remove related data first
                _context.Ingredients.RemoveRange(recipe.Ingredients);
                _context.RecipeSteps.RemoveRange(recipe.RecipeSteps);
                _context.Ratings.RemoveRange(recipe.Ratings);
                _context.UserFavorites.RemoveRange(recipe.Favorites);

                // Remove the recipe
                _context.Recipes.Remove(recipe);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Recipe '{recipe.Title}' has been deleted successfully.";
                return RedirectToPage("/Recipes/Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting recipe {RecipeId} by user {UserId}", id, userId);
                TempData["ErrorMessage"] = "An error occurred while deleting the recipe.";
                return RedirectToPage(new { id = id });
            }
        }

        // Submit rating - FIX THIS METHOD NAME
        public async Task<IActionResult> OnPostRateRecipeAsync(int id)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToPage("/Account/Login");
            }

            if (!ModelState.IsValid)
            {
                await OnGetAsync(id);
                return Page();
            }

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToPage("/Account/Login");
            }

            // Check if user owns this recipe
            var recipe = await _context.Recipes.FindAsync(id);
            if (recipe != null && recipe.UserID == userId)
            {
                TempData["ErrorMessage"] = "You cannot rate your own recipe.";
                return RedirectToPage(new { id = id });
            }

            try
            {
                // Check if user has already rated this recipe
                var existingRating = await _context.Ratings
                    .FirstOrDefaultAsync(r => r.UserID == userId && r.RecipeID == id);

                if (existingRating != null)
                {
                    // Update existing rating
                    existingRating.Score = Input.Rating;
                    existingRating.Comment = Input.Comment ?? string.Empty;
                    existingRating.RatingDate = DateTime.UtcNow;
                    TempData["SuccessMessage"] = "Your rating has been updated!";
                }
                else
                {
                    // Create new rating
                    var rating = new Rating
                    {
                        UserID = userId,
                        RecipeID = id,
                        Score = Input.Rating,
                        Comment = Input.Comment ?? string.Empty,
                        RatingDate = DateTime.UtcNow
                    };

                    _context.Ratings.Add(rating);
                    TempData["SuccessMessage"] = "Thank you for rating this recipe!";
                }

                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting rating for recipe {RecipeId} by user {UserId}", id, userId);
                TempData["ErrorMessage"] = "An error occurred while submitting your rating.";
            }

            return RedirectToPage(new { id = id });
        }
    }
}