using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RecipeSharingPlatform.Models;
using RecipeSharingPlatform.Pages.Admin;
using System.ComponentModel.DataAnnotations;

namespace RecipeSharingPlatform.Pages.Profile
{
    [Authorize]
    public class ChangePasswordModel : PageModel
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ILogger<ChangePasswordModel> _logger;

        public ChangePasswordModel(UserManager<User> userManager, SignInManager<User> signInManager, ILogger<ChangePasswordModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModelChangePassword Input { get; set; } = new();

        public void OnGet()
        {
            // Just display the form
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            try
            {
                // Verify current password
                var isCurrentPasswordValid = await _userManager.CheckPasswordAsync(user, Input.CurrentPassword);
                if (!isCurrentPasswordValid)
                {
                    ModelState.AddModelError("Input.CurrentPassword", "Current password is incorrect.");
                    return Page();
                }

                // Check if new password is different from current
                if (Input.NewPassword == Input.CurrentPassword)
                {
                    ModelState.AddModelError("Input.NewPassword", "New password must be different from your current password.");
                    return Page();
                }

                // Change password
                var result = await _userManager.ChangePasswordAsync(user, Input.CurrentPassword, Input.NewPassword);

                if (result.Succeeded)
                {
                    // Refresh sign-in to update security stamp
                    await _signInManager.RefreshSignInAsync(user);

                    TempData["SuccessMessage"] = "Your password has been changed successfully!";
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
                _logger.LogError(ex, "Error changing password for user {UserId}", user.Id);
                ModelState.AddModelError(string.Empty, "An error occurred while changing your password. Please try again.");
            }

            return Page();
        }
    }
}