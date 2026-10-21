namespace Domain;

// Lịch sử mục tiêu tuỳ chỉnh floor/target/ceiling của 1 nutrient: mỗi lần user đổi = một dòng mới có hiệu lực từ EffectiveFrom (ngày local của user), dòng cũ giữ nguyên.
// Ngày d dùng dòng có EffectiveFrom lớn nhất ≤ d; dòng cả ba null = "về mặc định" từ ngày đó; chưa có dòng nào ≤ d → mặc định (seed/DRI).
// Đổi nhiều lần trong cùng một ngày ghi đè dòng của ngày đó. Calories/P/F/C không nằm ở đây (dùng MacroProgramWeek).
public class UserNutrientTargetHistory : EntityBase<Guid>
{
    public Guid UserId { get; set; }
    public Guid NutrientDefinitionId { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public decimal? Floor { get; set; }
    public decimal? Target { get; set; }
    public decimal? Ceiling { get; set; }

    public User User { get; set; } = null!;
    public NutrientDefinition Nutrient { get; set; } = null!;
}
