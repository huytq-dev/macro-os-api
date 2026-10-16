namespace Domain;

// Bản chụp công thức tại một thời điểm; sửa Recipe không đổi lịch sử đã log
public class RecipeVersion : EntityBase<Guid>
{
    public Guid RecipeId { get; set; }
    public int VersionNo { get; set; }
    public decimal Servings { get; set; }
    public decimal? CookedYieldGrams { get; set; }  // khối lượng sau nấu: cơ sở tính khẩu phần
    public decimal? RawTotalGrams { get; set; }
    public string? Instructions { get; set; }
    public string IngredientsSnapshotJson { get; set; } = "[]";
    public decimal TotalCalories { get; set; }
    public decimal TotalProtein { get; set; }
    public decimal TotalCarbs { get; set; }
    public decimal TotalFat { get; set; }

    public Recipe Recipe { get; set; } = null!;
}
