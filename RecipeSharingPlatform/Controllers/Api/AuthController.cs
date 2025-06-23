using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using RecipeSharingPlatform.DTOs;
using RecipeSharingPlatform.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RecipeSharingPlatform.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IConfiguration configuration,
            ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // Find user by email or username
                var user = await _userManager.FindByEmailAsync(model.EmailOrUsername);
                if (user == null)
                {
                    user = await _userManager.FindByNameAsync(model.EmailOrUsername);
                }

                if (user == null)
                {
                    return Unauthorized(new { message = "Invalid login credentials" });
                }

                // Check password
                var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);
                if (!result.Succeeded)
                {
                    return Unauthorized(new { message = "Invalid login credentials" });
                }

                // Generate JWT token
                var token = await GenerateJwtToken(user);

                return Ok(new
                {
                    token = token,
                    user = new
                    {
                        id = user.Id,
                        username = user.UserName,
                        email = user.Email,
                        firstName = user.FirstName,
                        lastName = user.LastName,
                        role = user.Role
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during login");
                return StatusCode(500, new { message = "An error occurred during login" });
            }
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            try
            {
                // Validate role
                if (model.Role != "Chef" && model.Role != "User")
                {
                    return BadRequest(new { message = "Invalid role. Must be 'Chef' or 'User'" });
                }

                // Check if username exists
                var existingUserByUsername = await _userManager.FindByNameAsync(model.Username);
                if (existingUserByUsername != null)
                {
                    return BadRequest(new { message = "Username is already taken" });
                }

                // Check if email exists
                var existingUserByEmail = await _userManager.FindByEmailAsync(model.Email);
                if (existingUserByEmail != null)
                {
                    return BadRequest(new { message = "Email is already registered" });
                }

                // Create user
                var user = new User
                {
                    UserName = model.Username,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Role = model.Role,
                    CreatedDate = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Add user to role
                    await _userManager.AddToRoleAsync(user, model.Role);

                    // Generate JWT token
                    var token = await GenerateJwtToken(user);

                    return Ok(new
                    {
                        message = "Registration successful",
                        token = token,
                        user = new
                        {
                            id = user.Id,
                            username = user.UserName,
                            email = user.Email,
                            firstName = user.FirstName,
                            lastName = user.LastName,
                            role = user.Role
                        }
                    });
                }
                else
                {
                    return BadRequest(new { message = "Registration failed", errors = result.Errors });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during registration");
                return StatusCode(500, new { message = "An error occurred during registration" });
            }
        }

        private async Task<string> GenerateJwtToken(User user)
        {
            var roles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id),
                new(ClaimTypes.Name, user.UserName),
                new(ClaimTypes.Email, user.Email),
                new("FirstName", user.FirstName),
                new("LastName", user.LastName)
            };

            // Add role claims
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var jwtKey = _configuration["JwtSettings:SecretKey"];
            var key = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(24), // 24 hour expiry
                SigningCredentials = credentials
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}