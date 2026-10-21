namespace Infrastructure;

public class WorkoutMuscleAggregateConfiguration : BaseEntityConfiguration<WorkoutMuscleAggregate, Guid>
{
    public override void Configure(EntityTypeBuilder<WorkoutMuscleAggregate> builder)
    {
        base.Configure(builder);

        builder.ToTable("WorkoutMuscleAggregates");

        builder.Property(x => x.MuscleGroup).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.HardSets).HasPrecision(8, 2).IsRequired();
        builder.Property(x => x.VolumeKg).HasPrecision(12, 2).IsRequired();

        builder.HasIndex(x => new { x.UserId, x.PeriodStart, x.MuscleGroup }).IsUnique();

        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
