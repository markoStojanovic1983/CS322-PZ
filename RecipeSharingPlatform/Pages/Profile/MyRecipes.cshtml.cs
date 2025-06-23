using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RecipeSharingPlatform.Data;
using RecipeSharingPlatform.Models;
using Microsoft.EntityFrameworkCore;

namespace RecipeSharingPlatform.Pages.Profile
{
    [Authorize(Roles = "Chef")]
    public class MyRecipesModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<MyRecipesModel> _logger;

        public MyRecipesModel(ApplicationDbContext context, UserManager<User> userManager, ILogger<MyRecipesModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public List<RecipeWithStats> Recipes { get; set; } = new();
        public RecipeStatistics Stats { get; set; } = new();
        public string StatusFilter { get; set; } = "all";
        public string SearchTerm { get; set; } = string.Empty;

        public async Task OnGetAsync(string status = "all", string search = "")
        {
            StatusFilter = status?.ToLower() ?? "all";
            SearchTerm = search ?? string.Empty;

            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                await LoadRecipesAsync(user.Id);
                await LoadStatisticsAsync(user.Id);
            }
        }

        public async Task<IActionResult> OnPostDeleteAsync(int recipeId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound();
            }

            try
            {
                var recipe = await _context.Recipes
                    .Include(r => r.Ingredients)
                    .Include(r => r.RecipeSteps)
                    .Include(r => r.Ratings)
                    .Include(r => r.Favorites)
                    .FirstOrDefaultAsync(r => r.RecipeID == recipeId && r.UserID == user.Id);

                if (recipe == null)
                {
                    TempData["ErrorMessage"] = "Recipe not found or you don't have permission to delete it.";
                    return RedirectToPage();
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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting recipe with ID {RecipeId}", recipeId);
                TempData["ErrorMessage"] = "An error occurred while deleting the recipe. Please try again.";
            }

            return RedirectToPage();
        }

        // Helper method to load recipes
        private async Task LoadRecipesAsync(string userId)
        {
            var query = _context.Recipes
                .Include(r => r.Category)
                .Include(r => r.Ratings)
                .Include(r => r.Favorites)
                .Where(r => r.UserID == userId);

            // Apply status filter
            switch (StatusFilter)
            {
                case "approved":
                    query = query.Where(r => r.IsApproved);
                    break;
                case "pending":
                    query = query.Where(r => !r.IsApproved && !r.IsRejected);
                    break;
                case "rejected":
                    query = query.Where(r => r.IsRejected);
                    break;
                    // "all" - no additional filter
            }

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var searchLower = SearchTerm.ToLower();
                query = query.Where(r =>
                    r.Title.ToLower().Contains(searchLower) ||
                    r.Description.ToLower().Contains(searchLower) ||
                    r.Category.CategoryName.ToLower().Contains(searchLower));
            }

            var recipes = await query
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();

            Recipes = recipes.Select(r => new RecipeWithStats
            {
                Recipe = r,
                TotalRatings = r.Ratings.Count,
                AverageRating = r.Ratings.Any() ? r.Ratings.Average(rt => rt.Score) : 0,
                FavoriteCount = r.Favorites.Count,
                StatusBadge = GetStatusBadge(r),
                StatusColor = GetStatusColor(r)
            }).ToList();
        }

        // Helper method to load statistics
        private async Task LoadStatisticsAsync(string userId)
        {
            var allRecipes = await _context.Recipes
                .Include(r => r.Ratings)
                .Include(r => r.Favorites)
                .Where(r => r.UserID == userId)
                .ToListAsync();

            Stats.TotalRecipes = allRecipes.Count;
            Stats.ApprovedRecipes = allRecipes.Count(r => r.IsApproved);
            Stats.PendingRecipes = allRecipes.Count(r => !r.IsApproved && !r.IsRejected);
            Stats.RejectedRecipes = allRecipes.Count(r => r.IsRejected);

            var allRatings = allRecipes.SelectMany(r => r.Ratings).ToList();
            Stats.TotalRatingsReceived = allRatings.Count;
            Stats.OverallAverageRating = allRatings.Any() ? allRatings.Average(r => r.Score) : 0;

            Stats.TotalFavorites = allRecipes.Sum(r => r.Favorites.Count);
        }

        // Helper method to get status badge
        private static string GetStatusBadge(Recipe recipe)
        {
            if (recipe.IsApproved) return "Approved";
            if (recipe.IsRejected) return "Rejected";
            return "Pending";
        }

        // Helper method to get status color
        private static string GetStatusColor(Recipe recipe)
        {
            if (recipe.IsApproved) return "success";
            if (recipe.IsRejected) return "danger";
            return "warning";
        }
    }
}