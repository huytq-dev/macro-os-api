namespace Domain;

// Thiết bị có trong 1 phòng tập + bước tạ; progression chỉ gợi ý tải khớp IncrementKg
public class GymEquipment : EntityBase<Guid>
{
    public Guid GymProfileId { get; set; }
    public Guid EquipmentId { get; set; }
    public int? AvailableCount { get; set; }
    public decimal? IncrementKg { get; set; }
    public decimal? MinLoadKg { get; set; }
    public decimal? MaxLoadKg { get; set; }

    public GymProfile GymProfile { get; set; } = null!;
    public Equipment Equipment { get; set; } = null!;
}
