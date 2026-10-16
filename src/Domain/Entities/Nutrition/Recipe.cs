// Domain/Entities/Nutrition/Recipe.cs
using Domain;

public class Recipe : EntityBase<Guid>, ISoftDeletable
{
    public Guid CreatedByUserId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int ServingsCount { get; set; } = 1;
    public int? PrepTimeMinutes { get; set; }
    public int? CookTimeMinutes { get; set; }
    public MealType MealType { get; set; }
    public bool IsPublic { get; set; }
    public string? SourceUrl { get; set; }    // URL nguồn nếu import từ web
    public bool IsArchived { get; set; }
    public Guid? CurrentVersionId { get; set; }
    public string? TagsJson { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    // Calculated (denormalized for performance)
    public decimal TotalCalories { get; set; }
    public decimal TotalProtein { get; set; }
    public decimal TotalCarbs { get; set; }
    public decimal TotalFat { get; set; }

    // Navigation
    public User CreatedBy { get; set; } = null!;
    public ICollection<RecipeIngredient> Ingredients { get; set; } = [];
    public ICollection<FoodLogEntry> LogEntries { get; set; } = [];
}