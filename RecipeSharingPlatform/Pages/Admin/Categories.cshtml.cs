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
    public class CategoriesModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CategoriesModel> _logger;

        public CategoriesModel(ApplicationDbContext context, ILogger<CategoriesModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public List<CategoryWithStats> Categories { get; set; } = new();

        [BindProperty]
        public CategoryInput Input { get; set; } = new();

        [BindProperty]
        public int? EditingCategoryId { get; set; }

        public async Task OnGetAsync()
        {
            await LoadCategoriesAsync();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadCategoriesAsync();
                return Page();
            }

            try
            {
                // Check if category name already exists
                var existingCategory = await _context.Categories
                    .FirstOrDefaultAsync(c => c.CategoryName.ToLower() == Input.CategoryName.ToLower());

                if (existingCategory != null)
                {
                    ModelState.AddModelError("Input.CategoryName", "A category with this name already exists.");
                    await LoadCategoriesAsync();
                    return Page();
                }

                var category = new Category
                {
                    CategoryName = Input.CategoryName.Trim(),
                    Description = Input.Description?.Trim() ?? string.Empty
                };

                _context.Categories.Add(category);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Category '{category.CategoryName}' created successfully!";

                // Clear the form
                Input = new CategoryInput();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating a category.");
                TempData["ErrorMessage"] = "An error occurred while creating the category.";
            }

            await LoadCategoriesAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostEditAsync()
        {
            if (!EditingCategoryId.HasValue || !ModelState.IsValid)
            {
                await LoadCategoriesAsync();
                return Page();
            }

            try
            {
                var category = await _context.Categories.FindAsync(EditingCategoryId.Value);
                if (category == null)
                {
                    TempData["ErrorMessage"] = "Category not found.";
                    await LoadCategoriesAsync();
                    return Page();
                }

                // Check if new name conflicts with existing category (excluding current one)
                var existingCategory = await _context.Categories
                    .FirstOrDefaultAsync(c => c.CategoryName.ToLower() == Input.CategoryName.ToLower() && c.CategoryID != EditingCategoryId.Value);

                if (existingCategory != null)
                {
                    ModelState.AddModelError("Input.CategoryName", "A category with this name already exists.");
                    await LoadCategoriesAsync();
                    return Page();
                }

                category.CategoryName = Input.CategoryName.Trim();
                category.Description = Input.Description?.Trim() ?? string.Empty;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Category '{category.CategoryName}' updated successfully!";

                // Clear the form
                Input = new CategoryInput();
                EditingCategoryId = null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating a category.");
                TempData["ErrorMessage"] = "An error occurred while updating the category.";
            }

            await LoadCategoriesAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int categoryId)
        {
            try
            {
                var category = await _context.Categories
                    .Include(c => c.Recipes)
                    .FirstOrDefaultAsync(c => c.CategoryID == categoryId);

                if (category == null)
                {
                    TempData["ErrorMessage"] = "Category not found.";
                    await LoadCategoriesAsync();
                    return Page();
                }

                // Check if category has recipes
                if (category.Recipes.Any())
                {
                    TempData["ErrorMessage"] = $"Cannot delete '{category.CategoryName}' because it has {category.Recipes.Count} recipe(s). Move or delete the recipes first.";
                    await LoadCategoriesAsync();
                    return Page();
                }

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Category '{category.CategoryName}' deleted successfully!";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while deleting a category.");
                TempData["ErrorMessage"] = "An error occurred while deleting the category.";
            }

            await LoadCategoriesAsync();
            return Page();
        }

        public async Task<IActionResult> OnGetEditAsync(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                TempData["ErrorMessage"] = "Category not found.";
                return RedirectToPage();
            }

            EditingCategoryId = id;
            Input.CategoryName = category.CategoryName;
            Input.Description = category.Description ?? string.Empty;

            await LoadCategoriesAsync();
            return Page();
        }

        private async Task LoadCategoriesAsync()
        {
            var categories = await _context.Categories
                .Include(c => c.Recipes)
                .OrderBy(c => c.CategoryName)
                .ToListAsync();

            Categories = categories.Select(c => new CategoryWithStats
            {
                Category = c,
                RecipeCount = c.Recipes.Count,
                ApprovedRecipeCount = c.Recipes.Count(r => r.IsApproved),
                PendingRecipeCount = c.Recipes.Count(r => !r.IsApproved && !r.IsRejected)
            }).ToList();
        }
    }
}