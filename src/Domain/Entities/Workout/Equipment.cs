namespace Domain;

// Master thiết bị để gắn vào gym (khác enum EquipmentType trên Exercise)
public class Equipment : EntityBase<Guid>
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public bool IsLoadable { get; set; }
    public decimal? DefaultIncrementKg { get; set; }
}
