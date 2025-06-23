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
    public class RatingsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RatingsController> _logger;

        public RatingsController(ApplicationDbContext context, ILogger<RatingsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> CreateRating([FromBody] CreateRatingDto model)
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

                // Check if recipe exists and is approved
                var recipe = await _context.Recipes
                    .FirstOrDefaultAsync(r => r.RecipeID == model.RecipeId && r.IsApproved);

                if (recipe == null)
                {
                    return NotFound(new { message = "Recipe not found or not approved" });
                }

                // Check if user is not the recipe owner
                if (recipe.UserID == userId)
                {
                    return BadRequest(new { message = "You cannot rate your own recipe" });
                }

                // Check if user already rated this recipe
                var existingRating = await _context.Ratings
                    .FirstOrDefaultAsync(r => r.RecipeID == model.RecipeId && r.UserID == userId);

                if (existingRating != null)
                {
                    return BadRequest(new { message = "You have already rated this recipe. Use PUT to update your rating." });
                }

                // Create new rating
                var rating = new Rating
                {
                    RecipeID = model.RecipeId,
                    UserID = userId,
                    Score = model.Score,
                    Comment = model.Comment?.Trim() ?? string.Empty,
                    RatingDate = DateTime.UtcNow
                };

                _context.Ratings.Add(rating);
                await _context.SaveChangesAsync();

                var user = await _context.Users.FindAsync(userId);
                var ratingDto = new RatingDto
                {
                    RatingId = rating.RatingID,
                    RecipeId = rating.RecipeID,
                    RecipeTitle = recipe.Title,
                    UserName = $"{user.FirstName} {user.LastName}",
                    Score = rating.Score,
                    Comment = rating.Comment,
                    RatingDate = rating.RatingDate
                };

                return CreatedAtAction(nameof(GetRating), new { id = rating.RatingID },
                    new { message = "Rating created successfully", rating = ratingDto });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while creating rating");
                return StatusCode(500, new { message = "An error occurred while creating rating" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetRating(int id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var rating = await _context.Ratings
                    .Include(r => r.User)
                    .Include(r => r.Recipe)
                    .FirstOrDefaultAsync(r => r.RatingID == id);

                if (rating == null)
                {
                    return NotFound(new { message = "Rating not found" });
                }

                var ratingDto = new RatingDto
                {
                    RatingId = rating.RatingID,
                    RecipeId = rating.RecipeID,
                    RecipeTitle = rating.Recipe.Title,
                    UserName = $"{rating.User.FirstName} {rating.User.LastName}",
                    Score = rating.Score,
                    Comment = rating.Comment,
                    RatingDate = rating.RatingDate
                };

                return Ok(ratingDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving rating with ID {RatingId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving rating" });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateRating(int id, [FromBody] UpdateRatingDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var rating = await _context.Ratings
                    .Include(r => r.Recipe)
                    .FirstOrDefaultAsync(r => r.RatingID == id && r.UserID == userId);

                if (rating == null)
                {
                    return NotFound(new { message = "Rating not found or you don't have permission to update it" });
                }

                // Update rating
                rating.Score = model.Score;
                rating.Comment = model.Comment?.Trim() ?? string.Empty;
                rating.RatingDate = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var user = await _context.Users.FindAsync(userId);
                var ratingDto = new RatingDto
                {
                    RatingId = rating.RatingID,
                    RecipeId = rating.RecipeID,
                    RecipeTitle = rating.Recipe.Title,
                    UserName = $"{user.FirstName} {user.LastName}",
                    Score = rating.Score,
                    Comment = rating.Comment,
                    RatingDate = rating.RatingDate
                };

                return Ok(new { message = "Rating updated successfully", rating = ratingDto });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating rating with ID {RatingId}", id);
                return StatusCode(500, new { message = "An error occurred while updating rating" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRating(int id)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var rating = await _context.Ratings
                    .FirstOrDefaultAsync(r => r.RatingID == id && r.UserID == userId);

                if (rating == null)
                {
                    return NotFound(new { message = "Rating not found or you don't have permission to delete it" });
                }

                _context.Ratings.Remove(rating);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Rating deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while deleting rating with ID {RatingId}", id);
                return StatusCode(500, new { message = "An error occurred while deleting rating" });
            }
        }

        [HttpGet("recipe/{recipeId}")]
        public async Task<IActionResult> GetRecipeRatings(int recipeId, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                // Check if recipe exists and is approved
                var recipe = await _context.Recipes
                    .FirstOrDefaultAsync(r => r.RecipeID == recipeId && r.IsApproved);

                if (recipe == null)
                {
                    return NotFound(new { message = "Recipe not found or not approved" });
                }

                var query = _context.Ratings
                    .Include(r => r.User)
                    .Where(r => r.RecipeID == recipeId)
                    .OrderByDescending(r => r.RatingDate);

                var totalRatings = await query.CountAsync();
                var ratings = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var ratingDtos = ratings.Select(r => new RatingDto
                {
                    RatingId = r.RatingID,
                    RecipeId = r.RecipeID,
                    RecipeTitle = recipe.Title,
                    UserName = $"{r.User.FirstName} {r.User.LastName}",
                    Score = r.Score,
                    Comment = r.Comment,
                    RatingDate = r.RatingDate
                }).ToList();

                var averageRating = ratings.Any() ? ratings.Average(r => r.Score) : 0;

                return Ok(new
                {
                    ratings = ratingDtos,
                    totalCount = totalRatings,
                    averageRating = Math.Round(averageRating, 1),
                    currentPage = page,
                    pageSize = pageSize,
                    totalPages = (int)Math.Ceiling((double)totalRatings / pageSize)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving ratings for recipe with ID {RecipeId}", recipeId);
                return StatusCode(500, new { message = "An error occurred while retrieving recipe ratings" });
            }
        }

        [HttpGet("my")]
        public async Task<IActionResult> GetMyRatings([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User not found" });
                }

                var query = _context.Ratings
                    .Include(r => r.Recipe)
                    .Where(r => r.UserID == userId)
                    .OrderByDescending(r => r.RatingDate);

                var totalRatings = await query.CountAsync();
                var ratings = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var user = await _context.Users.FindAsync(userId);
                var ratingDtos = ratings.Select(r => new RatingDto
                {
                    RatingId = r.RatingID,
                    RecipeId = r.RecipeID,
                    RecipeTitle = r.Recipe.Title,
                    UserName = $"{user.FirstName} {user.LastName}",
                    Score = r.Score,
                    Comment = r.Comment,
                    RatingDate = r.RatingDate
                }).ToList();

                return Ok(new
                {
                    ratings = ratingDtos,
                    totalCount = totalRatings,
                    currentPage = page,
                    pageSize = pageSize,
                    totalPages = (int)Math.Ceiling((double)totalRatings / pageSize)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving ratings for user with ID {UserId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return StatusCode(500, new { message = "An error occurred while retrieving your ratings" });
            }
        }

        [HttpGet("user/{recipeId}")]
        public async Task<IActionResult> GetUserRatingForRecipe(int recipeId)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User not found" });
                }

                var rating = await _context.Ratings
                    .Include(r => r.Recipe)
                    .FirstOrDefaultAsync(r => r.RecipeID == recipeId && r.UserID == userId);

                if (rating == null)
                {
                    return Ok(new { hasRating = false });
                }

                var user = await _context.Users.FindAsync(userId);
                var ratingDto = new RatingDto
                {
                    RatingId = rating.RatingID,
                    RecipeId = rating.RecipeID,
                    RecipeTitle = rating.Recipe.Title,
                    UserName = $"{user.FirstName} {user.LastName}",
                    Score = rating.Score,
                    Comment = rating.Comment,
                    RatingDate = rating.RatingDate
                };

                return Ok(new { hasRating = true, rating = ratingDto });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving user rating for recipe with ID {RecipeId}", recipeId);
                return StatusCode(500, new { message = "An error occurred while retrieving user rating" });
            }
        }
    }
}