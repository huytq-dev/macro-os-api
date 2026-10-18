namespace Domain;

// Một thực phẩm "chuẩn" (Beef, Fish sauce…) gom nhiều bản ghi nguồn (USDA/OFF/VN/seed) về một khái niệm; mỗi canonical có đúng một FoodItem đại diện
public class CanonicalFood : EntityBase<Guid>
{
    public string Name { get; set; } = null!;               // tên chuẩn tiếng Anh
    public string NameNormalized { get; set; } = null!;     // lower + bỏ dấu + bỏ dấu câu; unique
    public string? NameVi { get; set; }
    public FoodCategory Category { get; set; }
    public CanonicalFoodStatus Status { get; set; } = CanonicalFoodStatus.Active;
    public decimal? DefaultServingGrams { get; set; }       // khẩu phần mặc định khi AI chỉ biết tên
    public decimal? DensityGPerMl { get; set; }

    public ICollection<FoodSourceRecord> Sources { get; set; } = [];
}
