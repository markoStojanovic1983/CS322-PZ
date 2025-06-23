// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using RecipeSharingPlatform.Models;

namespace RecipeSharingPlatform.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<User> _signInManager;
        private readonly UserManager<User> _userManager;
        private readonly ILogger<RegisterModel> _logger;

        public RegisterModel(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            ILogger<RegisterModel> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public List<SelectListItem> RoleList { get; set; } = new List<SelectListItem>();

        public class InputModel
        {
            [Required]
            [Display(Name = "First Name")]
            [StringLength(50)]
            public string FirstName { get; set; }

            [Required]
            [Display(Name = "Last Name")]
            [StringLength(50)]
            public string LastName { get; set; }

            [Required]
            [Display(Name = "Username")]
            [StringLength(50)]
            public string Username { get; set; }

            [Required]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required]
            [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Password")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirm password")]
            [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
            public string ConfirmPassword { get; set; }

            [Required]
            [Display(Name = "Role")]
            public string Role { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;

            // Create role dropdown (excluding Admin - only Chef and User can register)
            RoleList = new List<SelectListItem>
            {
                new SelectListItem { Value = "Chef", Text = "Chef (Can create and share recipes)" },
                new SelectListItem { Value = "User", Text = "User (Can browse and favorite recipes)" }
            };
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            // Recreate role list for validation
            RoleList = new List<SelectListItem>
            {
                new SelectListItem { Value = "Chef", Text = "Chef (Can create and share recipes)" },
                new SelectListItem { Value = "User", Text = "User (Can browse and favorite recipes)" }
            };

            if (ModelState.IsValid)
            {
                // Check if username already exists
                var existingUserByUsername = await _userManager.FindByNameAsync(Input.Username);
                if (existingUserByUsername != null)
                {
                    ModelState.AddModelError("Input.Username", "Username is already taken.");
                    return Page();
                }

                var user = new User
                {
                    UserName = Input.Username,
                    Email = Input.Email,
                    FirstName = Input.FirstName,
                    LastName = Input.LastName,
                    Role = Input.Role,
                    CreatedDate = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    // Add user to selected role
                    await _userManager.AddToRoleAsync(user, Input.Role);

                    _logger.LogInformation("User created a new account with password and role {Role}.", Input.Role);

                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return LocalRedirect(returnUrl);
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return Page();
        }
    }
}