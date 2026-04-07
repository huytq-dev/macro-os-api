namespace Infrastructure;

public class BodyMetricConfiguration : SoftDeleteEntityConfiguration<BodyMetric, Guid>
{
    public override void Configure(EntityTypeBuilder<BodyMetric> builder)
    {
        base.Configure(builder);

        builder.ToTable("BodyMetrics");

        builder.Property(x => x.Method)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.MeasuredTime).IsRequired(false);
        builder.Property(x => x.Notes).HasMaxLength(500).IsRequired(false);

        // Primary — WeightKg moved to WeightEntries table
        builder.Property(x => x.HeightCm).HasPrecision(5, 2).IsRequired(false);
        builder.Property(x => x.Bmi).HasPrecision(5, 2).IsRequired(false);

        // Body composition
        builder.Property(x => x.BodyFatPercent).HasPrecision(5, 2).IsRequired(false);
        builder.Property(x => x.MuscleMassKg).HasPrecision(5, 2).IsRequired(false);
        builder.Property(x => x.BoneMassKg).HasPrecision(5, 2).IsRequired(false);
        builder.Property(x => x.WaterPercent).HasPrecision(5, 2).IsRequired(false);
        builder.Property(x => x.VisceralFatLevel).HasPrecision(4, 1).IsRequired(false);

        // Circumferences
        builder.Property(x => x.WaistCm).HasPrecision(5, 2).IsRequired(false);
        builder.Property(x => x.HipCm).HasPrecision(5, 2).IsRequired(false);
        builder.Property(x => x.ChestCm).HasPrecision(5, 2).IsRequired(false);
        builder.Property(x => x.NeckCm).HasPrecision(5, 2).IsRequired(false);
        builder.Property(x => x.LeftArmCm).HasPrecision(5, 2).IsRequired(false);
        builder.Property(x => x.RightArmCm).HasPrecision(5, 2).IsRequired(false);
        builder.Property(x => x.LeftThighCm).HasPrecision(5, 2).IsRequired(false);
        builder.Property(x => x.RightThighCm).HasPrecision(5, 2).IsRequired(false);
        builder.Property(x => x.LeftCalfCm).HasPrecision(5, 2).IsRequired(false);
        builder.Property(x => x.RightCalfCm).HasPrecision(5, 2).IsRequired(false);

        // Vitals
        builder.Property(x => x.RestingHeartRate).IsRequired(false);
        builder.Property(x => x.BloodPressureSystolic).HasPrecision(5, 2).IsRequired(false);
        builder.Property(x => x.BloodPressureDiastolic).HasPrecision(5, 2).IsRequired(false);

        // Photos
        builder.Property(x => x.FrontPhotoUrl).HasMaxLength(1024).IsRequired(false);
        builder.Property(x => x.SidePhotoUrl).HasMaxLength(1024).IsRequired(false);
        builder.Property(x => x.BackPhotoUrl).HasMaxLength(1024).IsRequired(false);

        // --- Indexes ---
        builder.HasIndex(x => new { x.UserId, x.MeasuredDate });
    }
}