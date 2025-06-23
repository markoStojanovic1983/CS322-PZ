namespace RecipeSharingPlatform.DTOs
{
    public class FavoriteDto
    {
        public int RecipeId { get; set; }
        public string RecipeTitle { get; set; } = string.Empty;
        public string RecipeDescription { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string ChefName { get; set; } = string.Empty;
        public int PreparationTime { get; set; }
        public int CookingTime { get; set; }
        public int Servings { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public DateTime DateAdded { get; set; }
        public double AverageRating { get; set; }
        public int TotalRatings { get; set; }
        public int FavoriteCount { get; set; }
    }

    public class AddToFavoriteDto
    {
        public int RecipeId { get; set; }
    }
}