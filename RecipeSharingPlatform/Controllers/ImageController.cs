using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecipeSharingPlatform.Data;

namespace RecipeSharingPlatform.Controllers
{
    [Route("[controller]")]
    public class ImageController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ImageController> _logger;

        public ImageController(ApplicationDbContext context, ILogger<ImageController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Get recipe main image
        [HttpGet("GetRecipeImage/{id:int}")]
        public async Task<IActionResult> GetRecipeImage(int id)
        {
            try
            {
                var recipe = await _context.Recipes
                    .Where(r => r.RecipeID == id)
                    .Select(r => r.MainImage)
                    .FirstOrDefaultAsync();

                if (recipe == null || recipe.Length == 0)
                {
                    return NotFound("No image found for this recipe");
                }

                var contentType = GetImageContentType(recipe);
                return File(recipe, contentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading recipe image for ID {RecipeId}", id);
                return NotFound($"Error loading image: {ex.Message}");
            }
        }

        // Get recipe step image
        [HttpGet("GetStepImage/{id:int}")]
        public async Task<IActionResult> GetStepImage(int id)
        {
            try
            {
                var step = await _context.RecipeSteps
                    .Where(s => s.StepID == id)
                    .Select(s => s.StepImage)
                    .FirstOrDefaultAsync();

                if (step == null || step.Length == 0)
                {
                    return NotFound("No image found for this step");
                }

                var contentType = GetImageContentType(step);
                return File(step, contentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading step image for ID {StepId}", id);
                return NotFound($"Error loading step image: {ex.Message}");
            }
        }

        // Get user profile image - NEW for Week 6
        [HttpGet("GetUserProfileImage/{userId}")]
        public async Task<IActionResult> GetUserProfileImage(string userId)
        {
            try
            {
                var user = await _context.Users
                    .Where(u => u.Id == userId)
                    .Select(u => u.ProfileImage)
                    .FirstOrDefaultAsync();

                if (user == null || user.Length == 0)
                {
                    return NotFound("No profile image found for this user");
                }

                var contentType = GetImageContentType(user);
                return File(user, contentType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading profile image for user ID {UserId}", userId);
                return NotFound($"Error loading profile image: {ex.Message}");
            }
        }

        // Helper method to detect image content type from byte array
        private static string GetImageContentType(byte[] imageData)
        {
            if (imageData.Length < 8) return "image/jpeg"; // Default fallback

            // Check for WebP signature
            if (imageData.Length >= 12 &&
                imageData[0] == 0x52 && imageData[1] == 0x49 && imageData[2] == 0x46 && imageData[3] == 0x46 &&
                imageData[8] == 0x57 && imageData[9] == 0x45 && imageData[10] == 0x42 && imageData[11] == 0x50)
            {
                return "image/webp";
            }

            // Check for PNG signature
            if (imageData.Length >= 8 &&
                imageData[0] == 0x89 && imageData[1] == 0x50 && imageData[2] == 0x4E && imageData[3] == 0x47 &&
                imageData[4] == 0x0D && imageData[5] == 0x0A && imageData[6] == 0x1A && imageData[7] == 0x0A)
            {
                return "image/png";
            }

            // Check for JPEG signature
            if (imageData.Length >= 2 &&
                imageData[0] == 0xFF && imageData[1] == 0xD8)
            {
                return "image/jpeg";
            }

            // Default fallback
            return "image/jpeg";
        }

        // Image validation methods (existing)
        public static bool IsValidImage(IFormFile file)
        {
            if (file == null) return true; // Optional file is valid

            // Check file size (max 5MB = 5 * 1024 * 1024 bytes)
            const long maxFileSize = 5 * 1024 * 1024;
            if (file.Length > maxFileSize)
            {
                return false;
            }

            // Check file extension
            var extension = Path.GetExtension(file.FileName)?.ToLower();
            if (string.IsNullOrEmpty(extension))
            {
                return false;
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            if (!allowedExtensions.Contains(extension))
            {
                return false;
            }

            // Check MIME type for additional security
            var allowedMimeTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/webp" };
            if (!allowedMimeTypes.Contains(file.ContentType.ToLower()))
            {
                return false;
            }

            return true;
        }

        public static string ValidateImage(IFormFile file, string fieldName = "Image")
        {
            if (file == null) return null; // Optional file

            // Check file size (max 5MB)
            const long maxFileSize = 5 * 1024 * 1024;
            if (file.Length > maxFileSize)
            {
                var fileSizeMB = Math.Round(file.Length / 1024.0 / 1024.0, 2);
                return $"{fieldName} is {fileSizeMB}MB. Maximum allowed size is 5MB.";
            }

            // Check file extension
            var extension = Path.GetExtension(file.FileName)?.ToLower();
            if (string.IsNullOrEmpty(extension))
            {
                return $"{fieldName} must have a valid file extension.";
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            if (!allowedExtensions.Contains(extension))
            {
                return $"{fieldName} must be a JPG, PNG, or WebP file. Found: {extension}";
            }

            // Check MIME type
            var allowedMimeTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/webp" };
            if (!allowedMimeTypes.Contains(file.ContentType.ToLower()))
            {
                return $"{fieldName} has invalid content type: {file.ContentType}. Must be JPG, PNG, or WebP.";
            }

            return null; // No errors
        }

        // Helper method to convert file to byte array
        public static async Task<byte[]> ToByteArrayAsync(IFormFile file)
        {
            if (file == null) return null;

            using var stream = new MemoryStream();
            await file.CopyToAsync(stream);
            return stream.ToArray();
        }
    }
}