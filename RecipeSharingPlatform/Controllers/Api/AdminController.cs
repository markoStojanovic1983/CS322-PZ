using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecipeSharingPlatform.Data;
using RecipeSharingPlatform.DTOs;
using RecipeSharingPlatform.Models;

namespace RecipeSharingPlatform.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<RecipesController> _logger;


        public AdminController(ApplicationDbContext context, UserManager<User> userManager, ILogger<RecipesController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboardStats()
        {
            try
            {
                var stats = new
                {
                    TotalRecipes = await _context.Recipes.CountAsync(),
                    PendingRecipes = await _context.Recipes.CountAsync(r => !r.IsApproved && !r.IsRejected),
                    ApprovedRecipes = await _context.Recipes.CountAsync(r => r.IsApproved),
                    RejectedRecipes = await _context.Recipes.CountAsync(r => r.IsRejected),
                    TotalUsers = await _context.Users.CountAsync(),
                    TotalChefs = await _context.Users.CountAsync(u => u.Role == "Chef"),
                    TotalCategories = await _context.Categories.CountAsync(),
                    TotalRatings = await _context.Ratings.CountAsync(),
                    TotalFavorites = await _context.UserFavorites.CountAsync()
                };

                // Recent activity
                var recentRecipes = await _context.Recipes
                    .Include(r => r.User)
                    .Include(r => r.Category)
                    .OrderByDescending(r => r.CreatedDate)
                    .Take(10)
                    .Select(r => new
                    {
                        Type = "Recipe Submitted",
                        Description = $"{r.User.FirstName} {r.User.LastName} submitted \"{r.Title}\"",
                        Date = r.CreatedDate,
                        Status = r.IsApproved ? "Approved" : r.IsRejected ? "Rejected" : "Pending",
                        RecipeId = r.RecipeID
                    })
                    .ToListAsync();

                
                return Ok(new { statistics = stats, recentActivity = recentRecipes });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving dashboard data");
                return StatusCode(500, new { message = "An error occurred while retrieving dashboard data"});
            }
        }

        [HttpGet("recipes/pending")]
        public async Task<IActionResult> GetPendingRecipes()
        {
            try
            {
                var pendingRecipes = await _context.Recipes
                    .Include(r => r.User)
                    .Include(r => r.Category)
                    .Include(r => r.Ingredients)
                    .Include(r => r.RecipeSteps.OrderBy(s => s.StepNumber))
                    .Where(r => !r.IsApproved && !r.IsRejected)
                    .OrderBy(r => r.CreatedDate)
                    .ToListAsync();

                var recipeDtos = pendingRecipes.Select(r => new
                {
                    RecipeId = r.RecipeID,
                    Title = r.Title,
                    Description = r.Description,
                    PreparationTime = r.PreparationTime,
                    CookingTime = r.CookingTime,
                    Servings = r.Servings,
                    CategoryName = r.Category.CategoryName,
                    ChefName = $"{r.User.FirstName} {r.User.LastName}",
                    ChefEmail = r.User.Email,
                    ImageUrl = r.MainImage != null && r.MainImage.Length > 0
                        ? $"{Request.Scheme}://{Request.Host}/image/GetRecipeImage/{r.RecipeID}"
                        : string.Empty,
                    CreatedDate = r.CreatedDate,
                    ModifiedDate = r.ModifiedDate,
                    Ingredients = r.Ingredients.Select(i => new
                    {
                        Name = i.IngredientName,
                        Quantity = i.Quantity,
                        Unit = i.Unit
                    }).ToList(),
                    Steps = r.RecipeSteps.Select(s => new
                    {
                        StepNumber = s.StepNumber,
                        Description = s.StepDescription,
                        HasImage = s.StepImage != null && s.StepImage.Length > 0,
                        ImageUrl = s.StepImage != null && s.StepImage.Length > 0
                            ? $"{Request.Scheme}://{Request.Host}/image/GetStepImage/{s.StepID}"
                            : string.Empty
                    }).ToList()
                }).ToList();

                return Ok(new { pendingRecipes = recipeDtos, totalCount = recipeDtos.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pending recipes");
                return StatusCode(500, new { message = "An error occurred while retrieving pending recipes" });
            }
        }

        [HttpPost("recipes/{recipeId}/approve")]
        public async Task<IActionResult> ApproveRecipe(int recipeId, [FromBody] ModerationDto model)
        {
            try
            {
                var recipe = await _context.Recipes.FindAsync(recipeId);
                if (recipe == null)
                {
                    return NotFound(new { message = "Recipe not found" });
                }

                recipe.IsApproved = true;
                recipe.IsRejected = false;
                recipe.ModerationNotes = model?.Notes ?? string.Empty;

                await _context.SaveChangesAsync();
                return Ok(new { message = "Recipe approved successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving recipe with ID {RecipeId}", recipeId);
                return StatusCode(500, new { message = "An error occurred while approving the recipe"});
            }
        }

        [HttpPost("recipes/{recipeId}/reject")]
        public async Task<IActionResult> RejectRecipe(int recipeId, [FromBody] ModerationDto model)
        {
            if (string.IsNullOrWhiteSpace(model?.Notes))
            {
                return BadRequest(new { message = "Rejection reason is required" });
            }

            try
            {
                var recipe = await _context.Recipes.FindAsync(recipeId);
                if (recipe == null)
                {
                    return NotFound(new { message = "Recipe not found" });
                }

                recipe.IsApproved = false;
                recipe.IsRejected = true;
                recipe.ModerationNotes = model.Notes;

                await _context.SaveChangesAsync();
                return Ok(new { message = "Recipe rejected successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting recipe with ID {RecipeId}", recipeId);
                return StatusCode(500, new { message = "An error occurred while rejecting the recipe" });
            }
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetUsers([FromQuery] string search = "", [FromQuery] string roleFilter = "")
        {
            try
            {
                var query = _context.Users.AsQueryable();

                // Apply search filter
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var searchLower = search.ToLower();
                    query = query.Where(u =>
                        u.FirstName.ToLower().Contains(searchLower) ||
                        u.LastName.ToLower().Contains(searchLower) ||
                        u.Email.ToLower().Contains(searchLower) ||
                        u.UserName.ToLower().Contains(searchLower));
                }

                // Apply role filter
                if (!string.IsNullOrWhiteSpace(roleFilter) && roleFilter != "All")
                {
                    query = query.Where(u => u.Role == roleFilter);
                }

                var users = await query
                    .OrderBy(u => u.LastName)
                    .ThenBy(u => u.FirstName)
                    .ToListAsync();

                var userDtos = users.Select(u => new
                {
                    Id = u.Id,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Username = u.UserName,
                    Email = u.Email,
                    Role = u.Role,
                    CreatedDate = u.CreatedDate,
                    RecipeCount = _context.Recipes.Count(r => r.UserID == u.Id),
                    ProfileImageUrl = u.ProfileImage != null && u.ProfileImage.Length > 0
                        ? $"{Request.Scheme}://{Request.Host}/image/GetProfileImage/{u.Id}"
                        : string.Empty
                }).ToList();

                return Ok(new { users = userDtos, totalCount = userDtos.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users");
                return StatusCode(500, new { message = "An error occurred while retrieving users" });
            }
        }

        [HttpPut("users/{userId}/role")]
        public async Task<IActionResult> UpdateUserRole(string userId, [FromBody] UpdateUserRoleDto model)
        {
            if (string.IsNullOrWhiteSpace(model?.Role))
            {
                return BadRequest(new { message = "Role is required" });
            }

            if (!new[] { "User", "Chef", "Admin" }.Contains(model.Role))
            {
                _logger.LogWarning("Invalid role specified: {Role}", model.Role);
                return BadRequest(new { message = "Invalid role specified" });
            }

            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                // Remove user from all roles first
                var currentRoles = await _userManager.GetRolesAsync(user);
                if (currentRoles.Any())
                {
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                }

                // Add user to new role
                await _userManager.AddToRoleAsync(user, model.Role);

                // Update user role property
                user.Role = model.Role;
                await _userManager.UpdateAsync(user);

                return Ok(new { message = $"User role updated to {model.Role} successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating role for user with ID {UserId}", userId);
                return StatusCode(500, new { message = "An error occurred while updating the user role" });
            }
        }

        [HttpDelete("users/{userId}")]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                // Delete user's recipes and related data
                var userRecipes = await _context.Recipes
                    .Include(r => r.Ingredients)
                    .Include(r => r.RecipeSteps)
                    .Include(r => r.Ratings)
                    .Include(r => r.Favorites)
                    .Where(r => r.UserID == userId)
                    .ToListAsync();

                foreach (var recipe in userRecipes)
                {
                    _context.Ingredients.RemoveRange(recipe.Ingredients);
                    _context.RecipeSteps.RemoveRange(recipe.RecipeSteps);
                    _context.Ratings.RemoveRange(recipe.Ratings);
                    _context.UserFavorites.RemoveRange(recipe.Favorites);
                }
                _context.Recipes.RemoveRange(userRecipes);

                // Delete user's ratings and favorites
                var userRatings = await _context.Ratings.Where(r => r.UserID == userId).ToListAsync();
                var userFavorites = await _context.UserFavorites.Where(f => f.UserID == userId).ToListAsync();
                _context.Ratings.RemoveRange(userRatings);
                _context.UserFavorites.RemoveRange(userFavorites);

                await _context.SaveChangesAsync();

                // Delete the user
                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    return Ok(new { message = "User deleted successfully" });
                }
                else
                {
                    return StatusCode(500, new { message = "Failed to delete user", errors = result.Errors });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user with ID {UserId}", userId);
                return StatusCode(500, new { message = "An error occurred while deleting the user"});
            }
        }

        [HttpDelete("recipes/{recipeId}")]
        public async Task<IActionResult> DeleteRecipe(int recipeId)
        {
            try
            {
                var recipe = await _context.Recipes
                    .Include(r => r.Ingredients)
                    .Include(r => r.RecipeSteps)
                    .Include(r => r.Ratings)
                    .Include(r => r.Favorites)
                    .FirstOrDefaultAsync(r => r.RecipeID == recipeId);

                if (recipe == null)
                {
                    return NotFound(new { message = "Recipe not found" });
                }

                // Remove related data
                _context.Ingredients.RemoveRange(recipe.Ingredients);
                _context.RecipeSteps.RemoveRange(recipe.RecipeSteps);
                _context.Ratings.RemoveRange(recipe.Ratings);
                _context.UserFavorites.RemoveRange(recipe.Favorites);
                _context.Recipes.Remove(recipe);

                await _context.SaveChangesAsync();
                return Ok(new { message = "Recipe deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting recipe with ID {RecipeId}", recipeId);
                return StatusCode(500, new { message = "An error occurred while deleting the recipe" });
            }
        }
    }
}