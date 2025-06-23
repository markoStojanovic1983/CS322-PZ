namespace RecipeSharingPlatform.Models
{
    public class RecipeStatistics
    {
        public int TotalRecipes { get; set; }
        public int ApprovedRecipes { get; set; }
        public int PendingRecipes { get; set; }
        public int RejectedRecipes { get; set; }
        public double OverallAverageRating { get; set; }
        public int TotalRatingsReceived { get; set; }
        public int TotalFavorites { get; set; }
    }
}
