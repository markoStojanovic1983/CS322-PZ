using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RecipeSharingPlatform.Data;
using RecipeSharingPlatform.Models;
using Microsoft.EntityFrameworkCore;

namespace RecipeSharingPlatform.Pages.Profile
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public IndexModel(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public User CurrentUser { get; set; } = null!;
        public UserStatistics Stats { get; set; } = new();

        public async Task OnGetAsync()
        {
            // Get current user with related data
            CurrentUser = await _userManager.GetUserAsync(User);

            if (CurrentUser != null)
            {
                await LoadUserStatisticsAsync();
            }
        }

        // Helper method to load user statistics
        private async Task LoadUserStatisticsAsync()
        {
            // Basic user info
            Stats.MemberSince = CurrentUser.CreatedDate.ToString("MMMM yyyy");

            // Recipe statistics (for chefs)
            if (CurrentUser.Role == "Chef")
            {
                var recipes = await _context.Recipes
                    .Where(r => r.UserID == CurrentUser.Id)
                    .ToListAsync();

                Stats.TotalRecipes = recipes.Count;
                Stats.ApprovedRecipes = recipes.Count(r => r.IsApproved);
                Stats.PendingRecipes = recipes.Count(r => !r.IsApproved && !r.IsRejected);
                Stats.RejectedRecipes = recipes.Count(r => r.IsRejected);

                // Calculate average rating received on their recipes
                var allRatings = await _context.Ratings
                    .Where(r => recipes.Select(recipe => recipe.RecipeID).Contains(r.RecipeID))
                    .ToListAsync();

                Stats.TotalRatingsReceived = allRatings.Count;
                Stats.AverageRating = allRatings.Any() ? allRatings.Average(r => r.Score) : 0;
            }

            // Favorites count (for all users)
            Stats.TotalFavorites = await _context.UserFavorites
                .CountAsync(f => f.UserID == CurrentUser.Id);
        }
    }
}