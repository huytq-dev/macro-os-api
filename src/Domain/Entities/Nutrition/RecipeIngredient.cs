using Domain;

public class RecipeIngredient : EntityBase<Guid>
{
    public Guid RecipeId { get; set; }
    public Guid FoodItemId { get; set; }
    public Guid FoodServingId { get; set; }
    public decimal Quantity { get; set; }
    public int SortOrder { get; set; }
    public string? Notes { get; set; }
    public decimal? GramsNormalized { get; set; }           // quy đổi gram để tính yield
    public string? NutrientSnapshotJson { get; set; }       // snapshot lúc tạo công thức

    public Recipe Recipe { get; set; } = null!;
    public FoodItem FoodItem { get; set; } = null!;
    public FoodServing FoodServing { get; set; } = null!;
}