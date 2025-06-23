using Microsoft.AspNetCore.Identity;
using RecipeSharingPlatform.Models;

namespace RecipeSharingPlatform.Data
{
    public static class SeedData
    {
        public static async Task Initialize(IServiceProvider serviceProvider, UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
        {
            using var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            // Ensure database is created
            await context.Database.EnsureCreatedAsync();

            // Create roles
            await CreateRoles(roleManager);

            // Create users
            await CreateUsers(userManager);

            // Create categories
            await CreateCategories(context);

            await context.SaveChangesAsync();
        }

        private static async Task CreateRoles(RoleManager<IdentityRole> roleManager)
        {
            string[] roles = { "Admin", "Chef", "User" };

            foreach (string role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    var createRoleResult = await roleManager.CreateAsync(new IdentityRole(role));
                    if (!createRoleResult.Succeeded)
                    {
                        throw new Exception($"Failed to create role '{role}': {string.Join(", ", createRoleResult.Errors.Select(e => e.Description))}");
                    }
                }
            }
        }

        private static async Task CreateUsers(UserManager<User> userManager)
        {
            // Create Admin user
            if (await userManager.FindByEmailAsync("admin@recipesharingplatform.com") == null)
            {
                var admin = new User
                {
                    UserName = "admin",
                    Email = "admin@recipesharingplatform.com",
                    FirstName = "Admin",
                    LastName = "User",
                    Role = "Admin",
                    EmailConfirmed = true,
                    CreatedDate = DateTime.UtcNow
                };

                var createResult = await userManager.CreateAsync(admin, "Admin123!");
                if (createResult.Succeeded)
                {
                    // CRITICAL: Add role and verify it worked
                    var roleResult = await userManager.AddToRoleAsync(admin, "Admin");
                    if (!roleResult.Succeeded)
                    {
                        throw new Exception($"Failed to add Admin role to admin user: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
                    }

                    // Double-check role assignment
                    var roles = await userManager.GetRolesAsync(admin);
                    if (!roles.Contains("Admin"))
                    {
                        // Try one more time
                        await userManager.AddToRoleAsync(admin, "Admin");
                    }
                }
                else
                {
                    throw new Exception($"Failed to create admin user: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
                }
            }

            // Create Chef user
            if (await userManager.FindByEmailAsync("chef@recipesharingplatform.com") == null)
            {
                var chef = new User
                {
                    UserName = "chef",
                    Email = "chef@recipesharingplatform.com",
                    FirstName = "Gordon",
                    LastName = "Chef",
                    Role = "Chef",
                    EmailConfirmed = true,
                    CreatedDate = DateTime.UtcNow
                };

                var createResult = await userManager.CreateAsync(chef, "Chef123!");
                if (createResult.Succeeded)
                {
                    var roleResult = await userManager.AddToRoleAsync(chef, "Chef");
                    if (!roleResult.Succeeded)
                    {
                        throw new Exception($"Failed to add Chef role to chef user: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
                    }
                }
                else
                {
                    throw new Exception($"Failed to create chef user: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
                }
            }

            // Create regular User
            if (await userManager.FindByEmailAsync("user@recipesharingplatform.com") == null)
            {
                var user = new User
                {
                    UserName = "user",
                    Email = "user@recipesharingplatform.com",
                    FirstName = "John",
                    LastName = "Doe",
                    Role = "User",
                    EmailConfirmed = true,
                    CreatedDate = DateTime.UtcNow
                };

                var createResult = await userManager.CreateAsync(user, "User123!");
                if (createResult.Succeeded)
                {
                    var roleResult = await userManager.AddToRoleAsync(user, "User");
                    if (!roleResult.Succeeded)
                    {
                        throw new Exception($"Failed to add User role to regular user: {string.Join(", ", roleResult.Errors.Select(e => e.Description))}");
                    }
                }
                else
                {
                    throw new Exception($"Failed to create regular user: {string.Join(", ", createResult.Errors.Select(e => e.Description))}");
                }
            }
        }

        private static async Task CreateCategories(ApplicationDbContext context)
        {
            if (!context.Categories.Any())
            {
                var categories = new List<Category>
                {
                    new Category { CategoryName = "Appetizers", Description = "Start your meal with these delicious appetizers" },
                    new Category { CategoryName = "Main Courses", Description = "Hearty main dishes for every occasion" },
                    new Category { CategoryName = "Desserts", Description = "Sweet treats to end your meal" },
                    new Category { CategoryName = "Beverages", Description = "Refreshing drinks and cocktails" },
                    new Category { CategoryName = "Breakfast", Description = "Start your day right with these breakfast recipes" },
                    new Category { CategoryName = "Vegetarian", Description = "Delicious meat-free options" },
                    new Category { CategoryName = "Quick & Easy", Description = "Recipes ready in 30 minutes or less" }
                };

                context.Categories.AddRange(categories);
            }
        }
    }
}