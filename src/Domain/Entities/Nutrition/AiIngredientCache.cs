namespace Domain;

// Nhớ lựa chọn đã xác nhận của user cho một tên nguyên liệu AI: lần sau khỏi tìm lại. Theo user (món tự tạo là riêng tư)
public class AiIngredientCache : EntityBase<Guid>
{
    public Guid UserId { get; set; }
    public string NormalizedName { get; set; } = null!;
    public string Locale { get; set; } = null!;             // vi-VN, en-US
    public Guid? CanonicalFoodId { get; set; }
    public Guid? FoodItemId { get; set; }
    public int HitCount { get; set; }
    public DateTimeOffset LastUsedAt { get; set; }

    public User User { get; set; } = null!;
}
