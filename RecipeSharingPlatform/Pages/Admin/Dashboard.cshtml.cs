using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RecipeSharingPlatform.Data;
using RecipeSharingPlatform.Models;
using Microsoft.EntityFrameworkCore;

namespace RecipeSharingPlatform.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class DashboardModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DashboardModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Dashboard statistics
        public int TotalRecipes { get; set; }
        public int PendingRecipes { get; set; }
        public int ApprovedRecipes { get; set; }
        public int RejectedRecipes { get; set; }
        public int TotalUsers { get; set; }
        public int TotalChefs { get; set; }
        public int TotalCategories { get; set; }

        // Recent activity
        public List<RecentActivity> RecentActivities { get; set; } = new();

        public async Task OnGetAsync()
        {
            // Load statistics
            await LoadStatisticsAsync();

            // Load recent activity
            await LoadRecentActivityAsync();
        }

        // Helper method to load statistics
        private async Task LoadStatisticsAsync()
        {
            // Recipe statistics
            TotalRecipes = await _context.Recipes.CountAsync();
            PendingRecipes = await _context.Recipes.CountAsync(r => !r.IsApproved && !r.IsRejected);
            ApprovedRecipes = await _context.Recipes.CountAsync(r => r.IsApproved);
            RejectedRecipes = await _context.Recipes.CountAsync(r => r.IsRejected);

            // User statistics 
            TotalUsers = await _context.Users.CountAsync();
            TotalChefs = await _context.Users.CountAsync(u => u.Role == "Chef");

            // Category statistics
            TotalCategories = await _context.Categories.CountAsync();
        }

        // Helper method to load recent activity
        private async Task LoadRecentActivityAsync()
        {
            // Get recent recipe submissions (last 10)
            var recentRecipes = await _context.Recipes
                .Include(r => r.User)
                .Include(r => r.Category)
                .OrderByDescending(r => r.CreatedDate)
                .Take(10)
                .Select(r => new RecentActivity
                {
                    Type = "Recipe Submitted",
                    Description = $"{r.User.FirstName} {r.User.LastName} submitted \"{r.Title}\"",
                    Date = r.CreatedDate,
                    Status = r.IsApproved ? "Approved" : r.IsRejected ? "Rejected" : "Pending",
                    RecipeId = r.RecipeID
                })
                .ToListAsync();

            RecentActivities = recentRecipes;
        }
    }
}