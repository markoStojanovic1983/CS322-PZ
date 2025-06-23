using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace RecipeSharingPlatform.Models
{
    public class User : IdentityUser
    {
        [Required]
        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [StringLength(20)]
        public string Role { get; set; } = "User"; // Admin, Chef, User

        public byte[]? ProfileImage { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<Recipe> Recipes { get; set; } = new List<Recipe>();
        public virtual ICollection<UserFavorite> Favorites { get; set; } = new List<UserFavorite>();
        public virtual ICollection<Rating> Ratings { get; set; } = new List<Rating>();
    }
}