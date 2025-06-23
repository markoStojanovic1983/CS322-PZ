using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RecipeSharingPlatform.Models
{
    public class UserFavorite
    {
        [Required]
        public string UserID { get; set; } = string.Empty;

        [Required]
        public int RecipeID { get; set; }

        public DateTime DateAdded { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("UserID")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("RecipeID")]
        public virtual Recipe Recipe { get; set; } = null!;
    }
}