using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecipeSharingPlatform.Data;
using RecipeSharingPlatform.DTOs;
using RecipeSharingPlatform.Models;
using System.ComponentModel.DataAnnotations;

namespace RecipeSharingPlatform.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CategoriesController> _logger;

        public CategoriesController(ApplicationDbContext context, ILogger<CategoriesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
                var categories = await _context.Categories
                    .Include(c => c.Recipes)
                    .OrderBy(c => c.CategoryName)
                    .ToListAsync();

                var categoryDtos = categories.Select(c => new
                {
                    Id = c.CategoryID,
                    Name = c.CategoryName,
                    Description = c.Description,
                    RecipeCount = c.Recipes.Count,
                    ApprovedRecipeCount = c.Recipes.Count(r => r.IsApproved),
                    PendingRecipeCount = c.Recipes.Count(r => !r.IsApproved && !r.IsRejected)
                }).ToList();

                return Ok(new { categories = categoryDtos });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving categories");
                return StatusCode(500, new { message = "An error occurred while retrieving categories"});
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCategory(int id)
        {
            try
            {
                var category = await _context.Categories
                    .Include(c => c.Recipes)
                        .ThenInclude(r => r.User)
                    .FirstOrDefaultAsync(c => c.CategoryID == id);

                if (category == null)
                {
                    return NotFound(new { message = "Category not found" });
                }

                var categoryDto = new
                {
                    Id = category.CategoryID,
                    Name = category.CategoryName,
                    Description = category.Description,
                    RecipeCount = category.Recipes.Count,
                    ApprovedRecipeCount = category.Recipes.Count(r => r.IsApproved),
                    PendingRecipeCount = category.Recipes.Count(r => !r.IsApproved && !r.IsRejected),
                    Recipes = category.Recipes.Where(r => r.IsApproved).Select(r => new
                    {
                        Id = r.RecipeID,
                        Title = r.Title,
                        Description = r.Description,
                        ChefName = $"{r.User.FirstName} {r.User.LastName}",
                        ImageUrl = r.MainImage != null && r.MainImage.Length > 0
                            ? $"{Request.Scheme}://{Request.Host}/image/GetRecipeImage/{r.RecipeID}"
                            : string.Empty,
                        CreatedDate = r.CreatedDate
                    }).ToList()
                };

                return Ok(categoryDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving category with ID {CategoryId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving category"});
            }
        }

        [HttpPost]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // Check if category name already exists
                var existingCategory = await _context.Categories
                    .FirstOrDefaultAsync(c => c.CategoryName.ToLower() == model.Name.ToLower());

                if (existingCategory != null)
                {
                    return BadRequest(new { message = "A category with this name already exists" });
                }

                var category = new Category
                {
                    CategoryName = model.Name.Trim(),
                    Description = model.Description?.Trim() ?? string.Empty
                };

                _context.Categories.Add(category);
                await _context.SaveChangesAsync();

                var categoryDto = new
                {
                    Id = category.CategoryID,
                    Name = category.CategoryName,
                    Description = category.Description,
                    RecipeCount = 0
                };

                return CreatedAtAction(nameof(GetCategory), new { id = category.CategoryID },
                    new { message = "Category created successfully", category = categoryDto });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating category");
                return StatusCode(500, new { message = "An error occurred while creating the category"});
            }
        }

        [HttpPut("{id}")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateCategoryDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var category = await _context.Categories.FindAsync(id);
                if (category == null)
                {
                    return NotFound(new { message = "Category not found" });
                }

                // Check if new name conflicts with existing category (excluding current one)
                var existingCategory = await _context.Categories
                    .FirstOrDefaultAsync(c => c.CategoryName.ToLower() == model.Name.ToLower() && c.CategoryID != id);

                if (existingCategory != null)
                {
                    return BadRequest(new { message = "A category with this name already exists" });
                }

                category.CategoryName = model.Name.Trim();
                category.Description = model.Description?.Trim() ?? string.Empty;

                await _context.SaveChangesAsync();

                var categoryDto = new
                {
                    Id = category.CategoryID,
                    Name = category.CategoryName,
                    Description = category.Description
                };

                return Ok(new { message = "Category updated successfully", category = categoryDto });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category with ID {CategoryId}", id);
                return StatusCode(500, new { message = "An error occurred while updating the category" });
            }
        }

        [HttpDelete("{id}")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            try
            {
                var category = await _context.Categories
                    .Include(c => c.Recipes)
                    .FirstOrDefaultAsync(c => c.CategoryID == id);

                if (category == null)
                {
                    return NotFound(new { message = "Category not found" });
                }

                // Check if category has recipes
                if (category.Recipes.Any())
                {
                    return BadRequest(new
                    {
                        message = $"Cannot delete category with {category.Recipes.Count} recipe(s). Move or delete the recipes first."
                    });
                }

                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Category deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category with ID {CategoryId}", id);
                return StatusCode(500, new { message = "An error occurred while deleting the category"});
            }
        }
    }
}