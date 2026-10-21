namespace Domain;

// Pin Dashboard của 1 nutrient (PinOrder 1–8). Target tuỳ chỉnh floor/target/ceiling có lịch sử riêng: xem UserNutrientTargetHistory.
// Calories/P/F/C không nằm ở đây (dùng MacroProgramWeek).
public class UserNutrientTarget : EntityBase<Guid>
{
    public Guid UserId { get; set; }
    public Guid NutrientDefinitionId { get; set; }
    public byte? PinOrder { get; set; }     // 1–8; null = không pin

    public User User { get; set; } = null!;
    public NutrientDefinition Nutrient { get; set; } = null!;
}
