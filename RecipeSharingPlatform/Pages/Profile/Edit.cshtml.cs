using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RecipeSharingPlatform.Data;
using RecipeSharingPlatform.Models;
using RecipeSharingPlatform.Controllers;

namespace RecipeSharingPlatform.Pages.Profile
{
    [Authorize]
    public class EditModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly ILogger<EditModel> _logger;


        public EditModel(UserManager<User> userManager, ILogger<EditModel> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModelProfile Input { get; set; } = new();

        [BindProperty]
        public bool RemoveProfileImage { get; set; } = false;

        public bool HasCurrentProfileImage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Check if user has a profile image
            HasCurrentProfileImage = user.ProfileImage != null && user.ProfileImage.Length > 0;

            // Populate form with current user data
            Input.FirstName = user.FirstName;
            Input.LastName = user.LastName;
            Input.Username = user.UserName;
            Input.Email = user.Email;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            // Reload the current image status for the form
            HasCurrentProfileImage = user.ProfileImage != null && user.ProfileImage.Length > 0;

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Validate profile image if provided
            if (Input.ProfileImageFile != null)
            {
                var imageError = ImageController.ValidateImage(Input.ProfileImageFile, "Profile image");
                if (imageError != null)
                {
                    ModelState.AddModelError("Input.ProfileImageFile", imageError);
                    return Page();
                }
            }

            try
            {
                // Check if username is taken by another user
                if (Input.Username != user.UserName)
                {
                    var existingUser = await _userManager.FindByNameAsync(Input.Username);
                    if (existingUser != null && existingUser.Id != user.Id)
                    {
                        ModelState.AddModelError("Input.Username", "This username is already taken.");
                        return Page();
                    }
                }

                // Check if email is taken by another user
                if (Input.Email != user.Email)
                {
                    var existingUser = await _userManager.FindByEmailAsync(Input.Email);
                    if (existingUser != null && existingUser.Id != user.Id)
                    {
                        ModelState.AddModelError("Input.Email", "This email is already registered to another account.");
                        return Page();
                    }
                }

                // Update user properties
                user.FirstName = Input.FirstName.Trim();
                user.LastName = Input.LastName.Trim();
                user.UserName = Input.Username.Trim();
                user.Email = Input.Email.Trim();
                user.NormalizedUserName = Input.Username.Trim().ToUpper();
                user.NormalizedEmail = Input.Email.Trim().ToUpper();

                // Handle profile image
                if (RemoveProfileImage)
                {
                    user.ProfileImage = null;
                }
                else if (Input.ProfileImageFile != null)
                {
                    user.ProfileImage = await ImageController.ToByteArrayAsync(Input.ProfileImageFile);
                }

                // Update user in database
                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    TempData["SuccessMessage"] = "Profile updated successfully!";
                    return RedirectToPage("./Index");
                }
                else
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile for user {UserId}", user.Id);
                ModelState.AddModelError(string.Empty, "An error occurred while updating your profile. Please try again.");
            }

            return Page();
        }
    }
}