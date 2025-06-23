namespace RecipeSharingPlatform.DTOs
{
    public class RecipeDto
    {
        public int RecipeId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int PreparationTime { get; set; }
        public int CookingTime { get; set; }
        public int Servings { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string ChefName { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public bool IsApproved { get; set; }
        public bool IsRejected { get; set; }
        public string ModerationNotes { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public double AverageRating { get; set; }
        public int TotalRatings { get; set; }
        public int FavoriteCount { get; set; }
        public List<IngredientDto> Ingredients { get; set; } = new();
        public List<RecipeStepDto> Steps { get; set; } = new();
    }

    public class IngredientDto
    {
        public int IngredientId { get; set; }
        public string IngredientName { get; set; } = string.Empty;
        public string Quantity { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
    }

    public class RecipeStepDto
    {
        public int StepId { get; set; }
        public int StepNumber { get; set; }
        public string StepDescription { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
    }

    public class MyRecipeDto
    {
        public int RecipeId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // "Approved", "Pending", "Rejected"
        public string ModerationNotes { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public double AverageRating { get; set; }
        public int TotalRatings { get; set; }
        public int FavoriteCount { get; set; }
    }
}