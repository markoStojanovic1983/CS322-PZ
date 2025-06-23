namespace RecipeSharingPlatform.Models
{
    public class RecipeWithStats
    {
        public Recipe Recipe { get; set; } = null!;
        public int TotalRatings { get; set; }
        public double AverageRating { get; set; }
        public int FavoriteCount { get; set; }
        public string StatusBadge { get; set; } = string.Empty;
        public string StatusColor { get; set; } = string.Empty;
    }
}
