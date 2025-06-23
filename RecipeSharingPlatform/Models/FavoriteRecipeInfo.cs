namespace RecipeSharingPlatform.Models
{
    public class FavoriteRecipeInfo
    {
        public Recipe Recipe { get; set; } = null!;
        public DateTime DateAdded { get; set; }
        public double AverageRating { get; set; }
        public int TotalRatings { get; set; }
        public int FavoriteCount { get; set; }
    }
}
