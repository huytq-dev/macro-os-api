using Domain;

public class RecipeIngredient : EntityBase<Guid>
{
    public Guid RecipeId { get; set; }
    public Guid FoodItemId { get; set; }
    public Guid FoodServingId { get; set; }
    public decimal Quantity { get; set; }
    public int SortOrder { get; set; }
    public string? Notes { get; set; }

    public Recipe Recipe { get; set; } = null!;
    public FoodItem FoodItem { get; set; } = null!;
    public FoodServing FoodServing { get; set; } = null!;
}