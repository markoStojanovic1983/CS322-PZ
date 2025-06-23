namespace RecipeSharingPlatform.Models
{

    public class UserWithStats
    {
        public User User { get; set; } = null!;
        public int RecipeCount { get; set; }
        public int ApprovedRecipeCount { get; set; }
        public int PendingRecipeCount { get; set; }
        public int RejectedRecipeCount { get; set; }
        public DateTime LastLoginDate { get; set; }
    }

}
