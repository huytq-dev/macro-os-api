namespace Infrastructure;

public class ExerciseConfiguration : SoftDeleteEntityConfiguration<Exercise, Guid>
{
    public override void Configure(EntityTypeBuilder<Exercise> builder)
    {
        base.Configure(builder);

        builder.ToTable("Exercises");

        // --- Properties ---
        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(x => x.Description)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.Property(x => x.Instructions)
            .HasMaxLength(4000)
            .IsRequired(false);

        builder.Property(x => x.ImageUrl)
            .HasMaxLength(1024)
            .IsRequired(false);

        builder.Property(x => x.VideoUrl)
            .HasMaxLength(1024)
            .IsRequired(false);

        builder.Property(x => x.PrimaryMuscle)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Category)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Type)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Difficulty)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Equipment)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.IsCustom).HasDefaultValue(false);
        builder.Property(x => x.IsVerified).HasDefaultValue(false);
        builder.Property(x => x.CreatedByUserId).IsRequired(false);

        // --- Indexes ---
        builder.HasIndex(x => x.PrimaryMuscle);
        builder.HasIndex(x => x.Category);
        builder.HasIndex(x => x.Equipment);
        builder.HasIndex(x => x.Name);

        // --- Relationships ---
        builder.HasMany(x => x.SecondaryMuscles)
            .WithOne(x => x.Exercise)
            .HasForeignKey(x => x.ExerciseId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ExerciseMuscleConfiguration : BaseEntityConfiguration<ExerciseMuscle, Guid>
{
    public override void Configure(EntityTypeBuilder<ExerciseMuscle> builder)
    {
        base.Configure(builder);

        builder.ToTable("ExerciseMuscles");

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