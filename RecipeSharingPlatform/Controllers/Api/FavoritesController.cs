using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecipeSharingPlatform.Data;
using RecipeSharingPlatform.DTOs;
using RecipeSharingPlatform.Models;
using System.Security.Claims;

namespace RecipeSharingPlatform.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class FavoritesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FavoritesController> _logger;

        public FavoritesController(ApplicationDbContext context, ILogger<FavoritesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetFavorites([FromQuery] string search = "")
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User not found" });
                }

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
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var searchLower = search.ToLower();
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

                var favoriteDtos = favorites.Select(f => new FavoriteDto
                {
                    RecipeId = f.Recipe.RecipeID,
                    RecipeTitle = f.Recipe.Title,
                    RecipeDescription = f.Recipe.Description,
                    CategoryName = f.Recipe.Category.CategoryName,
                    ChefName = $"{f.Recipe.User.FirstName} {f.Recipe.User.LastName}",
                    PreparationTime = f.Recipe.PreparationTime,
                    CookingTime = f.Recipe.CookingTime,
                    Servings = f.Recipe.Servings,
                    ImageUrl = f.Recipe.MainImage != null && f.Recipe.MainImage.Length > 0
                        ? $"{Request.Scheme}://{Request.Host}/image/GetRecipeImage/{f.Recipe.RecipeID}"
                        : string.Empty,
                    DateAdded = f.DateAdded,
                    AverageRating = f.Recipe.Ratings.Any() ? f.Recipe.Ratings.Average(r => r.Score) : 0,
                    TotalRatings = f.Recipe.Ratings.Count,
                    FavoriteCount = f.Recipe.Favorites.Count
                }).ToList();

                return Ok(new { favorites = favoriteDtos, totalCount = favoriteDtos.Count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving favorites for user {UserId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return StatusCode(500, new { message = "An error occurred while retrieving favorites" });
            }
        }

        [HttpPost("{recipeId}")]
        public async Task<IActionResult> AddToFavorites(int recipeId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User not found" });
                }

                // Check if recipe exists and is approved
                var recipe = await _context.Recipes
                    .FirstOrDefaultAsync(r => r.RecipeID == recipeId && r.IsApproved);

                if (recipe == null)
                {
                    return NotFound(new { message = "Recipe not found or not approved" });
                }

                // Check if already in favorites
                var existingFavorite = await _context.UserFavorites
                    .FirstOrDefaultAsync(f => f.UserID == userId && f.RecipeID == recipeId);

                if (existingFavorite != null)
                {
                    return BadRequest(new { message = "Recipe is already in favorites" });
                }

                // Add to favorites
                var favorite = new UserFavorite
                {
                    UserID = userId,
                    RecipeID = recipeId,
                    DateAdded = DateTime.UtcNow
                };

                _context.UserFavorites.Add(favorite);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Recipe added to favorites successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding recipe {RecipeId} to favorites for user {UserId}", recipeId, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return StatusCode(500, new { message = "An error occurred while adding to favorites" });
            }
        }

        [HttpDelete("{recipeId}")]
        public async Task<IActionResult> RemoveFromFavorites(int recipeId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User not found" });
                }

                var favorite = await _context.UserFavorites
                    .FirstOrDefaultAsync(f => f.UserID == userId && f.RecipeID == recipeId);

                if (favorite == null)
                {
                    return NotFound(new { message = "Recipe not found in favorites" });
                }

                _context.UserFavorites.Remove(favorite);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Recipe removed from favorites successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing recipe {RecipeId} from favorites for user {UserId}", recipeId, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return StatusCode(500, new { message = "An error occurred while removing from favorites" });
            }
        }

        [HttpGet("check/{recipeId}")]
        public async Task<IActionResult> CheckIfFavorite(int recipeId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User not found" });
                }

                var isFavorite = await _context.UserFavorites
                    .AnyAsync(f => f.UserID == userId && f.RecipeID == recipeId);

                return Ok(new { isFavorite = isFavorite });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking favorite status for recipe {RecipeId} for user {UserId}", recipeId, User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return StatusCode(500, new { message = "An error occurred while checking favorite status" });
            }
        }
    }
}