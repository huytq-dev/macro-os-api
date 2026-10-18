namespace Domain;

// Nối CanonicalFood với bản ghi nguồn (FoodItem). Một FoodItem thuộc đúng một canonical
public class FoodSourceRecord : EntityBase<Guid>
{
    public Guid CanonicalFoodId { get; set; }
    public Guid FoodItemId { get; set; }
    public string Provider { get; set; } = null!;           // usda, off, vn-fct, seed
    public string? ExternalId { get; set; }
    public int Priority { get; set; }                       // nhỏ = ưu tiên hơn khi chọn đại diện/hiển thị
    public decimal MatchConfidence { get; set; }            // 0..1
    public string MatchMethod { get; set; } = null!;        // new, name, alias, barcode, seed, manual

    public CanonicalFood Canonical { get; set; } = null!;
    public FoodItem FoodItem { get; set; } = null!;
}
