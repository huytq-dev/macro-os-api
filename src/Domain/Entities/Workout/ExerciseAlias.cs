namespace Domain;

public class ExerciseAlias : EntityBase<Guid>
{
    public Guid ExerciseId { get; set; }
    public string Locale { get; set; } = null!;
    public string Alias { get; set; } = null!;
    public string AliasNormalized { get; set; } = null!;

    public Exercise Exercise { get; set; } = null!;
}
