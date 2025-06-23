namespace RecipeSharingPlatform.Models
{
    public class CategoryWithStats
    {
        public Category Category { get; set; } = null!;
        public int RecipeCount { get; set; }
        public int ApprovedRecipeCount { get; set; }
        public int PendingRecipeCount { get; set; }
    }
}
