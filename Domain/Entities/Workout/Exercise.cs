// Domain/Entities/Workout/Exercise.cs
using Domain;

public class Exercise : EntityBase<Guid>, ISoftDeletable
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? Instructions { get; set; }
    public string? ImageUrl { get; set; }
    public string? VideoUrl { get; set; }
    public MuscleGroup PrimaryMuscle { get; set; }
    public ExerciseCategory Category { get; set; }
    public ExerciseType Type { get; set; }          // Strength, Cardio, Flexibility
    public DifficultyLevel Difficulty { get; set; }
    public EquipmentType Equipment { get; set; }
    public bool IsCustom { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public bool IsVerified { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    // Navigation
    public ICollection<ExerciseMuscle> SecondaryMuscles { get; set; } = [];
    public ICollection<WorkoutTemplateExercise> TemplateExercises { get; set; } = [];
    public ICollection<WorkoutSessionExercise> SessionExercises { get; set; } = [];
}
