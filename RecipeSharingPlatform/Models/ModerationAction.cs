using System.ComponentModel.DataAnnotations;

namespace RecipeSharingPlatform.Models
{
    public class ModerationAction
    {
        [Required]
        public int RecipeId { get; set; }

        [Required]
        public string ActionType { get; set; } = string.Empty;

        [StringLength(500)]
        public string Notes { get; set; } = string.Empty;
    }
}
