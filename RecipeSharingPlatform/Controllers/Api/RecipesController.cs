using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecipeSharingPlatform.Data;
using RecipeSharingPlatform.DTOs;
using RecipeSharingPlatform.Models;
using RecipeSharingPlatform.Controllers;
using System.Security.Claims;

namespace RecipeSharingPlatform.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class RecipesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RecipesController> _logger;

        public RecipesController(ApplicationDbContext context, ILogger<RecipesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetRecipes([FromQuery] int page = 1, [FromQuery] int pageSize = 12, [FromQuery] string search = "", [FromQuery] int? categoryId = null)
        {
            try
            {
                var query = _context.Recipes
                    .Include(r => r.User)
                    .Include(r => r.Category)
                    .Include(r => r.Ratings)
                    .Include(r => r.Favorites)
                    .Include(r => r.Ingredients)
                    .Include(r => r.RecipeSteps.OrderBy(s => s.StepNumber))
                    .Where(r => r.IsApproved); // Only approved recipes for public API

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var searchLower = search.ToLower();
                    query = query.Where(r =>
                        r.Title.ToLower().Contains(searchLower) ||
                        r.Description.ToLower().Contains(searchLower) ||
                        r.Category.CategoryName.ToLower().Contains(searchLower));
                }

                // Apply category filter
                if (categoryId.HasValue)
                {
                    query = query.Where(r => r.CategoryID == categoryId.Value);
                }

                var totalRecipes = await query.CountAsync();
                var recipes = await query
                    .OrderByDescending(r => r.CreatedDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var recipeDtos = recipes.Select(r => new RecipeDto
                {
                    RecipeId = r.RecipeID,
                    Title = r.Title,
                    Description = r.Description,
                    PreparationTime = r.PreparationTime,
                    CookingTime = r.CookingTime,
                    Servings = r.Servings,
                    CategoryName = r.Category.CategoryName,
                    ChefName = $"{r.User.FirstName} {r.User.LastName}",
                    ImageUrl = r.MainImage != null && r.MainImage.Length > 0
                        ? $"{Request.Scheme}://{Request.Host}/image/GetRecipeImage/{r.RecipeID}"
                        : string.Empty,
                    IsApproved = r.IsApproved,
                    IsRejected = r.IsRejected,
                    ModerationNotes = r.ModerationNotes ?? string.Empty,
                    CreatedDate = r.CreatedDate,
                    ModifiedDate = r.ModifiedDate,
                    AverageRating = r.Ratings.Any() ? r.Ratings.Average(rt => rt.Score) : 0,
                    TotalRatings = r.Ratings.Count,
                    FavoriteCount = r.Favorites.Count,
                    Ingredients = r.Ingredients.Select(i => new IngredientDto
                    {
                        IngredientId = i.IngredientID,
                        IngredientName = i.IngredientName,
                        Quantity = i.Quantity,
                        Unit = i.Unit
                    }).ToList(),
                    Steps = r.RecipeSteps.Select(s => new RecipeStepDto
                    {
                        StepId = s.StepID,
                        StepNumber = s.StepNumber,
                        StepDescription = s.StepDescription,
                        ImageUrl = s.StepImage != null && s.StepImage.Length > 0
                            ? $"{Request.Scheme}://{Request.Host}/image/GetStepImage/{s.StepID}"
                            : string.Empty
                    }).ToList()
                }).ToList();

                return Ok(new
                {
                    recipes = recipeDtos,
                    totalCount = totalRecipes,
                    currentPage = page,
                    pageSize = pageSize,
                    totalPages = (int)Math.Ceiling((double)totalRecipes / pageSize)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recipes");
                return StatusCode(500, new { message = "An error occurred while retrieving recipes" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRecipe(int id)
        {
            try
            {
                var recipe = await _context.Recipes
                    .Include(r => r.User)
                    .Include(r => r.Category)
                    .Include(r => r.Ingredients)
                    .Include(r => r.RecipeSteps.OrderBy(s => s.StepNumber))
                    .Include(r => r.Ratings)
                    .Include(r => r.Favorites)
                    .FirstOrDefaultAsync(r => r.RecipeID == id);

                if (recipe == null)
                {
                    return NotFound(new { message = "Recipe not found" });
                }

                // Check if recipe is approved (unless user is owner or admin)
                if (!recipe.IsApproved)
                {
                    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    var isOwner = userId == recipe.UserID;
                    var isAdmin = User.IsInRole("Admin");

                    if (!isOwner && !isAdmin)
                    {
                        return NotFound(new { message = "Recipe not found or not approved" });
                    }
                }

                var recipeDto = new RecipeDto
                {
                    RecipeId = recipe.RecipeID,
                    Title = recipe.Title,
                    Description = recipe.Description,
                    PreparationTime = recipe.PreparationTime,
                    CookingTime = recipe.CookingTime,
                    Servings = recipe.Servings,
                    CategoryName = recipe.Category.CategoryName,
                    ChefName = $"{recipe.User.FirstName} {recipe.User.LastName}",
                    ImageUrl = recipe.MainImage != null && recipe.MainImage.Length > 0
                        ? $"{Request.Scheme}://{Request.Host}/image/GetRecipeImage/{recipe.RecipeID}"
                        : string.Empty,
                    IsApproved = recipe.IsApproved,
                    IsRejected = recipe.IsRejected,
                    ModerationNotes = recipe.ModerationNotes ?? string.Empty,
                    CreatedDate = recipe.CreatedDate,
                    ModifiedDate = recipe.ModifiedDate,
                    AverageRating = recipe.Ratings.Any() ? recipe.Ratings.Average(r => r.Score) : 0,
                    TotalRatings = recipe.Ratings.Count,
                    FavoriteCount = recipe.Favorites.Count,
                    Ingredients = recipe.Ingredients.Select(i => new IngredientDto
                    {
                        IngredientId = i.IngredientID,
                        IngredientName = i.IngredientName,
                        Quantity = i.Quantity,
                        Unit = i.Unit
                    }).ToList(),
                    Steps = recipe.RecipeSteps.Select(s => new RecipeStepDto
                    {
                        StepId = s.StepID,
                        StepNumber = s.StepNumber,
                        StepDescription = s.StepDescription,
                        ImageUrl = s.StepImage != null && s.StepImage.Length > 0
                            ? $"{Request.Scheme}://{Request.Host}/image/GetStepImage/{s.StepID}"
                            : string.Empty
                    }).ToList()
                };

                return Ok(recipeDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recipe with ID {RecipeId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving recipe" });
            }
        }

        [HttpGet("my")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Chef")]
        public async Task<IActionResult> GetMyRecipes([FromQuery] string status = "all", [FromQuery] string search = "", [FromQuery] int page = 1, [FromQuery] int pageSize = 12)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User not found" });
                }

                var query = _context.Recipes
                    .Include(r => r.Category)
                    .Include(r => r.Ratings)
                    .Include(r => r.Favorites)
                    .Where(r => r.UserID == userId);

                // Apply status filter
                switch (status.ToLower())
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
                }

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var searchLower = search.ToLower();
                    query = query.Where(r =>
                        r.Title.ToLower().Contains(searchLower) ||
                        r.Description.ToLower().Contains(searchLower) ||
                        r.Category.CategoryName.ToLower().Contains(searchLower));
                }

                var totalRecipes = await query.CountAsync();
                var recipes = await query
                    .OrderByDescending(r => r.CreatedDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var recipeDtos = recipes.Select(r => new MyRecipeDto
                {
                    RecipeId = r.RecipeID,
                    Title = r.Title,
                    Description = r.Description,
                    CategoryName = r.Category.CategoryName,
                    ImageUrl = r.MainImage != null && r.MainImage.Length > 0
                        ? $"{Request.Scheme}://{Request.Host}/image/GetRecipeImage/{r.RecipeID}"
                        : string.Empty,
                    Status = r.IsApproved ? "Approved" : r.IsRejected ? "Rejected" : "Pending",
                    ModerationNotes = r.ModerationNotes ?? string.Empty,
                    CreatedDate = r.CreatedDate,
                    ModifiedDate = r.ModifiedDate,
                    AverageRating = r.Ratings.Any() ? r.Ratings.Average(rt => rt.Score) : 0,
                    TotalRatings = r.Ratings.Count,
                    FavoriteCount = r.Favorites.Count
                }).ToList();

                // Get user statistics
                var allUserRecipes = await _context.Recipes
                    .Include(r => r.Ratings)
                    .Include(r => r.Favorites)
                    .Where(r => r.UserID == userId)
                    .ToListAsync();

                var stats = new
                {
                    TotalRecipes = allUserRecipes.Count,
                    ApprovedRecipes = allUserRecipes.Count(r => r.IsApproved),
                    PendingRecipes = allUserRecipes.Count(r => !r.IsApproved && !r.IsRejected),
                    RejectedRecipes = allUserRecipes.Count(r => r.IsRejected),
                    TotalRatingsReceived = allUserRecipes.SelectMany(r => r.Ratings).Count(),
                    OverallAverageRating = allUserRecipes.SelectMany(r => r.Ratings).Any()
                        ? allUserRecipes.SelectMany(r => r.Ratings).Average(r => r.Score) : 0,
                    TotalFavorites = allUserRecipes.Sum(r => r.Favorites.Count)
                };

                return Ok(new
                {
                    recipes = recipeDtos,
                    statistics = stats,
                    totalCount = totalRecipes,
                    currentPage = page,
                    pageSize = pageSize,
                    totalPages = (int)Math.Ceiling((double)totalRecipes / pageSize)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving my recipes for user {UserId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return StatusCode(500, new { message = "An error occurred while retrieving your recipes"});
            }
        }

        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            try
            {
                var categories = await _context.Categories
                    .Select(c => new { id = c.CategoryID, name = c.CategoryName, description = c.Description })
                    .OrderBy(c => c.name)
                    .ToListAsync();

                return Ok(new { categories = categories });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving categories");
                return StatusCode(500, new { message = "An error occurred while retrieving categories" });
            }
        }

        [HttpPost]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Chef")]
        public async Task<IActionResult> CreateRecipe([FromForm] CreateRecipeDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User not found" });
                }

                // Validate images
                var imageError = ValidateRecipeImages(model);
                if (imageError != null)
                {
                    return BadRequest(new { message = imageError });
                }

                // Validate ingredients and steps
                if (!model.Ingredients.Any(i => !string.IsNullOrWhiteSpace(i.IngredientName)))
                {
                    return BadRequest(new { message = "At least one ingredient is required" });
                }

                if (!model.Steps.Any(s => !string.IsNullOrWhiteSpace(s.StepDescription)))
                {
                    return BadRequest(new { message = "At least one step is required" });
                }

                // Create recipe
                var recipe = new Recipe
                {
                    Title = model.Title.Trim(),
                    Description = model.Description.Trim(),
                    PreparationTime = model.PreparationTime,
                    CookingTime = model.CookingTime,
                    Servings = model.Servings,
                    CategoryID = model.CategoryID,
                    UserID = userId,
                    CreatedDate = DateTime.UtcNow,
                    ModifiedDate = DateTime.UtcNow,
                    IsApproved = false,
                    IsRejected = false
                };

                // Handle main image
                if (model.MainImage != null)
                {
                    recipe.MainImage = await ImageController.ToByteArrayAsync(model.MainImage);
                }

                _context.Recipes.Add(recipe);
                await _context.SaveChangesAsync();

                // Add ingredients
                foreach (var ingredientDto in model.Ingredients.Where(i => !string.IsNullOrWhiteSpace(i.IngredientName)))
                {
                    var ingredient = new Ingredient
                    {
                        RecipeID = recipe.RecipeID,
                        IngredientName = ingredientDto.IngredientName.Trim(),
                        Quantity = ingredientDto.Quantity?.Trim() ?? "",
                        Unit = ingredientDto.Unit?.Trim() ?? ""
                    };
                    _context.Ingredients.Add(ingredient);
                }

                // Add steps
                for (int i = 0; i < model.Steps.Count; i++)
                {
                    var stepDto = model.Steps[i];
                    if (!string.IsNullOrWhiteSpace(stepDto.StepDescription))
                    {
                        var step = new RecipeStep
                        {
                            RecipeID = recipe.RecipeID,
                            StepNumber = i + 1,
                            StepDescription = stepDto.StepDescription.Trim()
                        };

                        // Handle step image
                        if (stepDto.StepImage != null)
                        {
                            step.StepImage = await ImageController.ToByteArrayAsync(stepDto.StepImage);
                        }

                        _context.RecipeSteps.Add(step);
                    }
                }

                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetRecipe), new { id = recipe.RecipeID },
                    new { message = "Recipe created successfully! It will be reviewed before being published.", recipeId = recipe.RecipeID });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating recipe for user");
                return StatusCode(500, new { message = "An error occurred while creating the recipe" });
            }
        }

        [HttpPut("{id}")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Chef")]
        public async Task<IActionResult> UpdateRecipe(int id, [FromForm] UpdateRecipeDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var recipe = await _context.Recipes
                    .Include(r => r.Ingredients)
                    .Include(r => r.RecipeSteps)
                    .FirstOrDefaultAsync(r => r.RecipeID == id && r.UserID == userId);

                if (recipe == null)
                {
                    return NotFound(new { message = "Recipe not found or you don't have permission to edit it" });
                }

                // Validate images
                var imageError = ValidateRecipeImages(model);
                if (imageError != null)
                {
                    return BadRequest(new { message = imageError });
                }

                // Validate ingredients and steps
                if (!model.Ingredients.Any(i => !string.IsNullOrWhiteSpace(i.IngredientName)))
                {
                    return BadRequest(new { message = "At least one ingredient is required" });
                }

                if (!model.Steps.Any(s => !string.IsNullOrWhiteSpace(s.StepDescription)))
                {
                    return BadRequest(new { message = "At least one step is required" });
                }

                // Update basic recipe properties
                recipe.Title = model.Title.Trim();
                recipe.Description = model.Description.Trim();
                recipe.PreparationTime = model.PreparationTime;
                recipe.CookingTime = model.CookingTime;
                recipe.Servings = model.Servings;
                recipe.CategoryID = model.CategoryID;
                recipe.ModifiedDate = DateTime.UtcNow;

                // Reset approval status when recipe is modified
                recipe.IsApproved = false;
                recipe.IsRejected = false;
                recipe.ModerationNotes = null;

                // Handle main image
                if (model.RemoveMainImage)
                {
                    recipe.MainImage = null;
                }
                else if (model.MainImage != null)
                {
                    recipe.MainImage = await ImageController.ToByteArrayAsync(model.MainImage);
                }

                // Remove existing ingredients and steps
                _context.Ingredients.RemoveRange(recipe.Ingredients);
                _context.RecipeSteps.RemoveRange(recipe.RecipeSteps);

                // Add new ingredients
                foreach (var ingredientDto in model.Ingredients.Where(i => !string.IsNullOrWhiteSpace(i.IngredientName)))
                {
                    var ingredient = new Ingredient
                    {
                        RecipeID = recipe.RecipeID,
                        IngredientName = ingredientDto.IngredientName.Trim(),
                        Quantity = ingredientDto.Quantity?.Trim() ?? "",
                        Unit = ingredientDto.Unit?.Trim() ?? ""
                    };
                    _context.Ingredients.Add(ingredient);
                }

                // Add new steps
                for (int i = 0; i < model.Steps.Count; i++)
                {
                    var stepDto = model.Steps[i];
                    if (!string.IsNullOrWhiteSpace(stepDto.StepDescription))
                    {
                        var step = new RecipeStep
                        {
                            RecipeID = recipe.RecipeID,
                            StepNumber = i + 1,
                            StepDescription = stepDto.StepDescription.Trim()
                        };

                        // Handle step image
                        if (stepDto.StepImage != null)
                        {
                            step.StepImage = await ImageController.ToByteArrayAsync(stepDto.StepImage);
                        }

                        _context.RecipeSteps.Add(step);
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = "Recipe updated successfully!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating recipe with ID {RecipeId}", id);
                return StatusCode(500, new { message = "An error occurred while updating the recipe"});
            }
        }

        [HttpDelete("{id}")]
        [Authorize(AuthenticationSchemes = "Bearer", Roles = "Chef")]
        public async Task<IActionResult> DeleteRecipe(int id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var recipe = await _context.Recipes
                    .Include(r => r.Ingredients)
                    .Include(r => r.RecipeSteps)
                    .Include(r => r.Ratings)
                    .Include(r => r.Favorites)
                    .FirstOrDefaultAsync(r => r.RecipeID == id && r.UserID == userId);

                if (recipe == null)
                {
                    return NotFound(new { message = "Recipe not found or you don't have permission to delete it" });
                }

                // Remove related data
                _context.Ingredients.RemoveRange(recipe.Ingredients);
                _context.RecipeSteps.RemoveRange(recipe.RecipeSteps);
                _context.Ratings.RemoveRange(recipe.Ratings);
                _context.UserFavorites.RemoveRange(recipe.Favorites);

                // Remove the recipe
                _context.Recipes.Remove(recipe);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Recipe deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting recipe with ID {RecipeId}", id);
                return StatusCode(500, new { message = "An error occurred while deleting the recipe" });
            }
        }

        private string ValidateRecipeImages(dynamic model)
        {
            // Check main image
            if (model.MainImage != null)
            {
                var mainImageError = ImageController.ValidateImage(model.MainImage, "Main image");
                if (mainImageError != null)
                    return mainImageError;
            }

            // Check step images
            for (int i = 0; i < model.Steps.Count; i++)
            {
                if (model.Steps[i].StepImage != null)
                {
                    var stepImageError = ImageController.ValidateImage(model.Steps[i].StepImage, $"Step {i + 1} image");
                    if (stepImageError != null)
                        return stepImageError;
                }
            }

            return null;
        }
    }
}