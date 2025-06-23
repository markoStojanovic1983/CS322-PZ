using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RecipeSharingPlatform.Data;
using RecipeSharingPlatform.Models;
using Microsoft.EntityFrameworkCore;

namespace RecipeSharingPlatform.Pages.Profile
{
    [Authorize]
    public class FavoritesModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<FavoritesModel> _logger;

        public FavoritesModel(ApplicationDbContext context, UserManager<User> userManager, ILogger<FavoritesModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public List<FavoriteRecipeInfo> FavoriteRecipes { get; set; } = new();
        public string SearchTerm { get; set; } = string.Empty;
        public int TotalFavorites { get; set; }

        public async Task OnGetAsync(string search = "")
        {
            SearchTerm = search ?? string.Empty;

            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                await LoadFavoriteRecipesAsync(user.Id);
            }
        }

        public async Task<IActionResult> OnPostRemoveFromFavoritesAsync(int recipeId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            try
            {
                var favorite = await _context.UserFavorites
                    .FirstOrDefaultAsync(f => f.UserID == user.Id && f.RecipeID == recipeId);

                if (favorite != null)
                {
                    _context.UserFavorites.Remove(favorite);
                    await _context.SaveChangesAsync();

                    var recipe = await _context.Recipes.FindAsync(recipeId);
                    TempData["SuccessMessage"] = $"'{recipe?.Title}' has been removed from your favorites.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Recipe not found in your favorites.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing recipe from favorites for user {UserId}", user.Id);
                TempData["ErrorMessage"] = "An error occurred while removing the recipe from favorites.";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostAddToFavoritesAsync(int recipeId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            try
            {
                // Check if already in favorites
                var existingFavorite = await _context.UserFavorites
                    .FirstOrDefaultAsync(f => f.UserID == user.Id && f.RecipeID == recipeId);

                if (existingFavorite == null)
                {
                    var favorite = new UserFavorite
                    {
                        UserID = user.Id,
                        RecipeID = recipeId,
                        DateAdded = DateTime.UtcNow
                    };

                    _context.UserFavorites.Add(favorite);
                    await _context.SaveChangesAsync();

                    var recipe = await _context.Recipes.FindAsync(recipeId);
                    TempData["SuccessMessage"] = $"'{recipe?.Title}' has been added to your favorites!";
                }
                else
                {
                    TempData["ErrorMessage"] = "Recipe is already in your favorites.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding recipe to favorites for user {UserId}", user.Id);
                TempData["ErrorMessage"] = "An error occurred while adding the recipe to favorites.";
            }

            return RedirectToPage();
        }

        // Helper method to load favorite recipes with search filter
        private async Task LoadFavoriteRecipesAsync(string userId)
        {
            var query = _context.UserFavorites
                .Include(f => f.Recipe)
                    .ThenInclude(r => r.User)
                .Include(f => f.Recipe)
                    .ThenInclude(r => r.Category)
                .Include(f => f.Recipe)
                    .ThenInclude(r => r.Ratings)
                .Include(f => f.Recipe)
                    .ThenInclude(r => r.Favorites)
                .Where(f => f.UserID == userId)
                .Where(f => f.Recipe.IsApproved); // Only show approved recipes

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var searchLower = SearchTerm.ToLower();
                query = query.Where(f =>
                    f.Recipe.Title.ToLower().Contains(searchLower) ||
                    f.Recipe.Description.ToLower().Contains(searchLower) ||
                    f.Recipe.Category.CategoryName.ToLower().Contains(searchLower) ||
                    f.Recipe.User.FirstName.ToLower().Contains(searchLower) ||
                    f.Recipe.User.LastName.ToLower().Contains(searchLower));
            }

            var favorites = await query
                .OrderByDescending(f => f.DateAdded)
                .ToListAsync();

            FavoriteRecipes = favorites.Select(f => new FavoriteRecipeInfo
            {
                Recipe = f.Recipe,
                DateAdded = f.DateAdded,
                AverageRating = f.Recipe.Ratings.Any() ? f.Recipe.Ratings.Average(r => r.Score) : 0,
                TotalRatings = f.Recipe.Ratings.Count,
                FavoriteCount = f.Recipe.Favorites.Count
            }).ToList();

            TotalFavorites = favorites.Count;
        }
    }
}