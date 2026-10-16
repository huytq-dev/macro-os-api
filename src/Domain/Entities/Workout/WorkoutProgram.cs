namespace Domain;

// Chương trình tập; nội dung nằm ở WorkoutProgramRevision (sửa = tạo revision mới, session cũ giữ revision cũ)
public class WorkoutProgram : EntityBase<Guid>, ISoftDeletable
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = null!;
    public WorkoutGoal Goal { get; set; }
    public ProgramCreationMode CreationMode { get; set; }
    public WorkoutProgramStatus Status { get; set; } = WorkoutProgramStatus.Draft;
    public Guid? CurrentRevisionId { get; set; }
    public DateOnly? StartedAt { get; set; }
    public string? Icon { get; set; }
    public string? Color { get; set; }
    public byte[] RowVersion { get; set; } = null!;
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public User User { get; set; } = null!;
    public ICollection<WorkoutProgramRevision> Revisions { get; set; } = [];
}
