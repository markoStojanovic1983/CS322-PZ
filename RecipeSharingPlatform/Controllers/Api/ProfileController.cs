using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecipeSharingPlatform.Controllers;
using RecipeSharingPlatform.Data;
using RecipeSharingPlatform.DTOs;
using RecipeSharingPlatform.Models;
using System.Security.Claims;

namespace RecipeSharingPlatform.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(AuthenticationSchemes = "Bearer")]
    public class ProfileController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(ApplicationDbContext context, UserManager<User> userManager, ILogger<ProfileController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized(new { message = "User not found" });
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                // Get user statistics
                var stats = await GetUserStatistics(userId, user.Role);

                var profileDto = new ProfileDto
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Username = user.UserName,
                    Email = user.Email,
                    Role = user.Role,
                    ProfileImageUrl = user.ProfileImage != null && user.ProfileImage.Length > 0
                        ? $"{Request.Scheme}://{Request.Host}/image/GetUserProfileImage/{user.Id}"
                        : string.Empty,
                    CreatedDate = user.CreatedDate
                };

                return Ok(new { profile = profileDto, statistics = stats });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving profile for user {UserId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return StatusCode(500, new { message = "An error occurred while retrieving profile" });
            }
        }

        [HttpPut]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                // Check if username is taken by another user
                if (model.Username != user.UserName)
                {
                    var existingUser = await _userManager.FindByNameAsync(model.Username);
                    if (existingUser != null && existingUser.Id != user.Id)
                    {
                        return BadRequest(new { message = "Username is already taken" });
                    }
                }

                // Check if email is taken by another user
                if (model.Email != user.Email)
                {
                    var existingUser = await _userManager.FindByEmailAsync(model.Email);
                    if (existingUser != null && existingUser.Id != user.Id)
                    {
                        return BadRequest(new { message = "Email is already registered to another account" });
                    }
                }

                // Update user properties
                user.FirstName = model.FirstName.Trim();
                user.LastName = model.LastName.Trim();
                user.UserName = model.Username.Trim();
                user.Email = model.Email.Trim();
                user.NormalizedUserName = model.Username.Trim().ToUpper();
                user.NormalizedEmail = model.Email.Trim().ToUpper();

                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    var profileDto = new ProfileDto
                    {
                        Id = user.Id,
                        FirstName = user.FirstName,
                        LastName = user.LastName,
                        Username = user.UserName,
                        Email = user.Email,
                        Role = user.Role,
                        ProfileImageUrl = user.ProfileImage != null && user.ProfileImage.Length > 0
                            ? $"{Request.Scheme}://{Request.Host}/image/GetUserProfileImage/{user.Id}"
                            : string.Empty,
                        CreatedDate = user.CreatedDate
                    };

                    return Ok(new { message = "Profile updated successfully", profile = profileDto });
                }
                else
                {
                    return BadRequest(new { message = "Profile update failed", errors = result.Errors });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating profile for user {UserId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return StatusCode(500, new { message = "An error occurred while updating profile" });
            }
        }

        [HttpPost("image")]
        public async Task<IActionResult> UploadProfileImage(IFormFile image)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                if (image == null)
                {
                    return BadRequest(new { message = "No image file provided" });
                }

                // Validate image
                var imageError = ImageController.ValidateImage(image, "Profile image");
                if (imageError != null)
                {
                    return BadRequest(new { message = imageError });
                }

                // Convert to byte array and save
                user.ProfileImage = await ImageController.ToByteArrayAsync(image);
                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    var imageUrl = $"{Request.Scheme}://{Request.Host}/image/GetUserProfileImage/{user.Id}";
                    return Ok(new { message = "Profile image updated successfully", imageUrl = imageUrl });
                }
                else
                {
                    return BadRequest(new { message = "Failed to update profile image" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while uploading profile image for user {UserId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return StatusCode(500, new { message = "An error occurred while uploading profile image" });
            }
        }

        [HttpDelete("image")]
        public async Task<IActionResult> RemoveProfileImage()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                user.ProfileImage = null;
                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    return Ok(new { message = "Profile image removed successfully" });
                }
                else
                {
                    return BadRequest(new { message = "Failed to remove profile image" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while removing profile image for user {UserId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return StatusCode(500, new { message = "An error occurred while removing profile image" });
            }
        }

        [HttpPut("password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                // Verify current password
                var isCurrentPasswordValid = await _userManager.CheckPasswordAsync(user, model.CurrentPassword);
                if (!isCurrentPasswordValid)
                {
                    return BadRequest(new { message = "Current password is incorrect" });
                }

                // Check if new password is different from current
                if (model.NewPassword == model.CurrentPassword)
                {
                    return BadRequest(new { message = "New password must be different from current password" });
                }

                // Change password
                var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

                if (result.Succeeded)
                {
                    return Ok(new { message = "Password changed successfully" });
                }
                else
                {
                    return BadRequest(new { message = "Password change failed", errors = result.Errors });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while changing password for user {UserId}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return StatusCode(500, new { message = "An error occurred while changing password"});
            }
        }

        private async Task<object> GetUserStatistics(string userId, string role)
        {
            if (role == "Chef")
            {
                var recipes = await _context.Recipes
                    .Where(r => r.UserID == userId)
                    .ToListAsync();

                var allRatings = await _context.Ratings
                    .Where(r => recipes.Select(recipe => recipe.RecipeID).Contains(r.RecipeID))
                    .ToListAsync();

                var totalFavorites = await _context.UserFavorites
                    .CountAsync(f => f.UserID == userId);

                return new
                {
                    TotalRecipes = recipes.Count,
                    ApprovedRecipes = recipes.Count(r => r.IsApproved),
                    PendingRecipes = recipes.Count(r => !r.IsApproved && !r.IsRejected),
                    RejectedRecipes = recipes.Count(r => r.IsRejected),
                    TotalRatingsReceived = allRatings.Count,
                    AverageRating = allRatings.Any() ? allRatings.Average(r => r.Score) : 0,
                    TotalFavorites = totalFavorites,
                    MemberSince = recipes.Any() ? recipes.Min(r => r.CreatedDate).ToString("MMMM yyyy") : "No recipes yet"
                };
            }
            else
            {
                var totalFavorites = await _context.UserFavorites
                    .CountAsync(f => f.UserID == userId);

                var user = await _userManager.FindByIdAsync(userId);

                return new
                {
                    TotalFavorites = totalFavorites,
                    MemberSince = user?.CreatedDate.ToString("MMMM yyyy") ?? "Unknown"
                };
            }
        }
    }
}