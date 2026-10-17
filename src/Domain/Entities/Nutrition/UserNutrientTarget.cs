namespace Domain;

// Target tuỳ chỉnh + pin Dashboard của 1 nutrient. Cột null → dùng NutrientDefinition.ReferenceMin/Target/Max.
// Dòng chỉ có PinOrder = pin không đổi target. Calories/P/F/C không nằm ở đây (dùng MacroProgramWeek).
public class UserNutrientTarget : EntityBase<Guid>
{
    public Guid UserId { get; set; }
    public Guid NutrientDefinitionId { get; set; }
    public decimal? Floor { get; set; }
    public decimal? Target { get; set; }
    public decimal? Ceiling { get; set; }
    public byte? PinOrder { get; set; }     // 1–8; null = không pin

    public User User { get; set; } = null!;
    public NutrientDefinition Nutrient { get; set; } = null!;
}
