namespace Domain;

/// <summary>
/// Preset yêu thích kiểu MacroFactor: món + khẩu phần + số lượng đã chọn lúc bấm tim.
/// Cùng một món có thể có nhiều preset (vd. ly 12 oz và 16 oz). FoodServingId null = Quantity là gram.
/// </summary>
public class FoodFavorite : EntityBase<Guid>
{
    public Guid UserId { get; set; }
    public Guid FoodItemId { get; set; }
    public Guid? FoodServingId { get; set; }
    public decimal Quantity { get; set; } = 1;
    public int SortOrder { get; set; }

    public User User { get; set; } = null!;
    public FoodItem FoodItem { get; set; } = null!;
    public FoodServing? FoodServing { get; set; }
}
