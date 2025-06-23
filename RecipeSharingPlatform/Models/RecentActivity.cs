namespace RecipeSharingPlatform.Models
{
    public class RecentActivity
    {
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Status { get; set; } = string.Empty;
        public int? RecipeId { get; set; }
    }
}
