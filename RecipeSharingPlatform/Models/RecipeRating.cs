namespace RecipeSharingPlatform.Models
{

    public class RecipeRating
    {
        public string UserName { get; set; } = string.Empty;
        public int Score { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime RatingDate { get; set; }
    }
}
