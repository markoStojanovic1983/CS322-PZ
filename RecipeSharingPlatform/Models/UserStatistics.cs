namespace RecipeSharingPlatform.Models
{
    public class UserStatistics
    {
        public int TotalRecipes { get; set; }
        public int ApprovedRecipes { get; set; }
        public int PendingRecipes { get; set; }
        public int RejectedRecipes { get; set; }
        public int TotalFavorites { get; set; }
        public double AverageRating { get; set; }
        public int TotalRatingsReceived { get; set; }
        public string MemberSince { get; set; } = string.Empty;
    }
}
