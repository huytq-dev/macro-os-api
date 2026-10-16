namespace Domain;

public class GymProfile : EntityBase<Guid>, ISoftDeletable
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = null!;
    public bool IsDefault { get; set; }
    public string? Notes { get; set; }
    public byte[] RowVersion { get; set; } = null!;
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public User User { get; set; } = null!;
    public ICollection<GymEquipment> Equipment { get; set; } = [];
    public ICollection<GymExerciseOverride> ExerciseOverrides { get; set; } = [];
}
