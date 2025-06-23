using System.ComponentModel.DataAnnotations;

namespace RecipeSharingPlatform.DTOs
{

    public class UpdateUserRoleDto
    {
        [Required]
        public string Role { get; set; } = string.Empty;
    }
}
