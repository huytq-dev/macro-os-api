namespace Infrastructure;

public class PersonalRecordConfiguration : BaseEntityConfiguration<PersonalRecord, Guid>
{
    public override void Configure(EntityTypeBuilder<PersonalRecord> builder)
    {
        base.Configure(builder);

        builder.ToTable("PersonalRecords");

        builder.Property(x => x.Metric).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.Value).HasPrecision(10, 3).IsRequired();

        builder.HasIndex(x => new { x.UserId, x.ExerciseId, x.Metric });

        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Exercise).WithMany().HasForeignKey(x => x.ExerciseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ExerciseSet>().WithMany().HasForeignKey(x => x.ExerciseSetId)
            .OnDelete(DeleteBehavior.Restrict).IsRequired(false);
    }
}
