using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using RecipeSharingPlatform.Data;
using RecipeSharingPlatform.Models;

namespace RecipeSharingPlatform.Pages.Recipes
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

        public List<RecipeWithStats> Recipes { get; set; } = new();
        public List<Category> Categories { get; set; } = new();

        // Search and filter properties
        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; } = string.Empty;

        [BindProperty(SupportsGet = true)]
        public int? CategoryFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SortBy { get; set; } = "newest";

        [BindProperty(SupportsGet = true)]
        public int? MaxCookTime { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? MaxPrepTime { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? Servings { get; set; }

        // Statistics
        public int TotalRecipes { get; set; }
        public int FilteredCount { get; set; }

        public async Task OnGetAsync()
        {
            try
            {
                // Load categories for filter dropdown
                Categories = await _context.Categories
                    .OrderBy(c => c.CategoryName)
                    .ToListAsync();

                // Get total recipe count
                TotalRecipes = await _context.Recipes.CountAsync(r => r.IsApproved);

                // Build query for approved recipes
                var query = _context.Recipes
                    .Include(r => r.User)
                    .Include(r => r.Category)
                    .Include(r => r.Ratings)
                    .Include(r => r.Favorites)
                    .Where(r => r.IsApproved && !r.IsRejected);

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(SearchTerm))
                {
                    var searchLower = SearchTerm.ToLower();
                    query = query.Where(r =>
                        r.Title.ToLower().Contains(searchLower) ||
                        r.Description.ToLower().Contains(searchLower) ||
                        r.Category.CategoryName.ToLower().Contains(searchLower) ||
                        r.User.FirstName.ToLower().Contains(searchLower) ||
                        r.User.LastName.ToLower().Contains(searchLower));
                }

                // Apply category filter
                if (CategoryFilter.HasValue)
                {
                    query = query.Where(r => r.CategoryID == CategoryFilter.Value);
                }



                // Apply sorting
                query = SortBy switch
                {
                    "oldest" => query.OrderBy(r => r.CreatedDate),
                    "title" => query.OrderBy(r => r.Title),
                    "rating" => query.OrderByDescending(r => r.Ratings.Any() ? r.Ratings.Average(rt => rt.Score) : 0),
                    _ => query.OrderByDescending(r => r.CreatedDate) // "newest" - default
                };

                var recipes = await query.ToListAsync();

                // Map to RecipeWithStats
                Recipes = recipes.Select(r => new RecipeWithStats
                {
                    Recipe = r,
                    AverageRating = r.Ratings.Any() ? r.Ratings.Average(rt => rt.Score) : 0,
                    TotalRatings = r.Ratings.Count,
                    FavoriteCount = r.Favorites.Count
                }).ToList();

                FilteredCount = Recipes.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading recipes on Index page");
                // Handle errors gracefully
                Recipes = new List<RecipeWithStats>();
                TotalRecipes = 0;
                FilteredCount = 0;
                // You could add logging here
            }
        }

        public SelectList GetCategorySelectList()
        {
            var items = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "All Categories" }
            };

            items.AddRange(Categories.Select(c => new SelectListItem
            {
                Value = c.CategoryID.ToString(),
                Text = c.CategoryName,
                Selected = CategoryFilter == c.CategoryID
            }));

            return new SelectList(items, "Value", "Text", CategoryFilter?.ToString() ?? "");
        }

        public SelectList GetSortSelectList()
        {
            var items = new List<SelectListItem>
            {
                new SelectListItem { Value = "newest", Text = "Newest First" },
                new SelectListItem { Value = "oldest", Text = "Oldest First" },
                new SelectListItem { Value = "title", Text = "Recipe Name A-Z" },
                new SelectListItem { Value = "rating", Text = "Highest Rated" }
            };

            return new SelectList(items, "Value", "Text", SortBy);
        }
    }
}