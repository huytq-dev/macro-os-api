namespace Infrastructure;

public class ExerciseMuscleConfiguration : BaseEntityConfiguration<ExerciseMuscle, Guid>
{
    public override void Configure(EntityTypeBuilder<ExerciseMuscle> builder)
    {
        base.Configure(builder);

        builder.ToTable("ExerciseMuscles");
        builder.Property(x => x.ContributionWeight).HasPrecision(4, 3).IsRequired(false);

        builder.Property(x => x.MuscleGroup)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Role)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        // Unique: 1 muscle group / role per exercise
        builder.HasIndex(x => new { x.ExerciseId, x.MuscleGroup, x.Role })
            .IsUnique();
    }
}
