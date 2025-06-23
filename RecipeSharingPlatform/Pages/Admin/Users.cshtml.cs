using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RecipeSharingPlatform.Data;
using RecipeSharingPlatform.Models;
using Microsoft.EntityFrameworkCore;

namespace RecipeSharingPlatform.Pages.Admin
{
    [Authorize(Roles = "Admin")]
    public class UsersModel : PageModel
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<UsersModel> _logger;

        public UsersModel(ApplicationDbContext context, UserManager<User> userManager, ILogger<UsersModel> logger)
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public List<UserWithStats> Users { get; set; } = new();
        public string SearchTerm { get; set; } = string.Empty;
        public string RoleFilter { get; set; } = string.Empty;

        public async Task OnGetAsync(string searchTerm = "", string roleFilter = "")
        {
            SearchTerm = searchTerm ?? string.Empty;
            RoleFilter = roleFilter ?? string.Empty;

            await LoadUsersAsync();
        }

        public async Task<IActionResult> OnPostChangeRoleAsync(string userId, string newRole)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToPage();
                }

                // Don't allow changing your own role
                var currentUser = await _userManager.GetUserAsync(User);
                if (user.Id == currentUser.Id)
                {
                    TempData["ErrorMessage"] = "You cannot change your own role.";
                    return RedirectToPage();
                }

                // Validate new role
                if (!new[] { "Admin", "Chef", "User" }.Contains(newRole))
                {
                    TempData["ErrorMessage"] = "Invalid role specified.";
                    return RedirectToPage();
                }

                // Remove from all roles and add to new role
                var currentRoles = await _userManager.GetRolesAsync(user);
                if (currentRoles.Any())
                {
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                }

                await _userManager.AddToRoleAsync(user, newRole);

                // Update user's role property
                user.Role = newRole;
                await _userManager.UpdateAsync(user);

                TempData["SuccessMessage"] = $"User '{user.FirstName} {user.LastName}' role changed to {newRole}.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing user role for user {UserId}", userId);
                TempData["ErrorMessage"] = "An error occurred while changing the user role.";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostToggleAccountAsync(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "User not found.";
                    return RedirectToPage();
                }

                // Don't allow disabling your own account
                var currentUser = await _userManager.GetUserAsync(User);
                if (user.Id == currentUser.Id)
                {
                    TempData["ErrorMessage"] = "You cannot disable your own account.";
                    return RedirectToPage();
                }

                if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTimeOffset.UtcNow)
                {
                    // Enable user
                    user.LockoutEnd = null;
                    await _userManager.UpdateAsync(user);
                    TempData["SuccessMessage"] = $"User '{user.FirstName} {user.LastName}' account enabled.";
                }
                else
                {
                    // Disable user
                    user.LockoutEnd = DateTimeOffset.UtcNow.AddYears(100);
                    await _userManager.UpdateAsync(user);
                    TempData["SuccessMessage"] = $"User '{user.FirstName} {user.LastName}' account disabled.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling user account for user {UserId}", userId);
                TempData["ErrorMessage"] = "An error occurred while updating the user account.";
            }

            return RedirectToPage();
        }

        // Helper method to load users with statistics
        private async Task LoadUsersAsync()
        {
            var query = _context.Users.AsQueryable();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                var searchLower = SearchTerm.ToLower();
                query = query.Where(u =>
                    u.FirstName.ToLower().Contains(searchLower) ||
                    u.LastName.ToLower().Contains(searchLower) ||
                    u.Email.ToLower().Contains(searchLower) ||
                    u.UserName.ToLower().Contains(searchLower));
            }

            // Apply role filter
            if (!string.IsNullOrWhiteSpace(RoleFilter))
            {
                query = query.Where(u => u.Role == RoleFilter);
            }

            var users = await query
                .Include(u => u.Recipes)
                .OrderBy(u => u.LastName)
                .ThenBy(u => u.FirstName)
                .ToListAsync();

            Users = users.Select(u => new UserWithStats
            {
                User = u,
                RecipeCount = u.Recipes.Count,
                ApprovedRecipeCount = u.Recipes.Count(r => r.IsApproved),
                PendingRecipeCount = u.Recipes.Count(r => !r.IsApproved && !r.IsRejected),
                RejectedRecipeCount = u.Recipes.Count(r => r.IsRejected),
                LastLoginDate = u.CreatedDate
            }).ToList();
        }
    }
}