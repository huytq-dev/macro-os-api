namespace Infrastructure;

public class GymExerciseOverrideConfiguration : BaseEntityConfiguration<GymExerciseOverride, Guid>
{
    public override void Configure(EntityTypeBuilder<GymExerciseOverride> builder)
    {
        base.Configure(builder);

        builder.ToTable("GymExerciseOverrides");

        builder.Property(x => x.Rule).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(500).IsRequired(false);

        builder.HasIndex(x => new { x.GymProfileId, x.ExerciseId }).IsUnique();

        builder.HasOne(x => x.GymProfile).WithMany(x => x.ExerciseOverrides).HasForeignKey(x => x.GymProfileId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Exercise).WithMany().HasForeignKey(x => x.ExerciseId).OnDelete(DeleteBehavior.Restrict);
    }
}
