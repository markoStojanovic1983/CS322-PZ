using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using RecipeSharingPlatform.Data;
using RecipeSharingPlatform.Models;

namespace RecipeSharingPlatform.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ApplicationDbContext context, ILogger<IndexModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public List<Recipe> FeaturedRecipes { get; set; } = new();

        public async Task OnGetAsync()
        {
            try
            {
                // Load top 3 featured recipes based on ratings and favorites
                // Priority: highest rated recipes with at least 1 rating, then newest approved recipes
                FeaturedRecipes = await _context.Recipes
                    .Include(r => r.User)
                    .Include(r => r.Category)
                    .Include(r => r.Ratings)
                    .Include(r => r.Favorites)
                    .Where(r => r.IsApproved && !r.IsRejected)
                    .OrderByDescending(r => r.Ratings.Any() ? r.Ratings.Average(rt => rt.Score) : 0)
                    .ThenByDescending(r => r.Favorites.Count)
                    .ThenByDescending(r => r.CreatedDate)
                    .Take(3)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading featured recipes for home page");
                FeaturedRecipes = new List<Recipe>();
            }
        }
    }
}