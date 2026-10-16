namespace Domain;

public class ExerciseEquipment : EntityBase<Guid>
{
    public Guid ExerciseId { get; set; }
    public Guid EquipmentId { get; set; }
    public EquipmentRequirement Requirement { get; set; } = EquipmentRequirement.Required;

    public Exercise Exercise { get; set; } = null!;
    public Equipment Equipment { get; set; } = null!;
}
