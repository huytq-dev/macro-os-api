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
        builder.Property(x => x.ShouldersCm).HasPrecision(5, 2).IsRequired(false);
        builder.Property(x => x.BustCm).HasPrecision(5, 2).IsRequired(false);
        builder.Property(x => x.LeftForearmCm).HasPrecision(5, 2).IsRequired(false);
        builder.Property(x => x.RightForearmCm).HasPrecision(5, 2).IsRequired(false);
        builder.Property(x => x.LeftWristCm).HasPrecision(5, 2).IsRequired(false);
        builder.Property(x => x.RightWristCm).HasPrecision(5, 2).IsRequired(false);
        builder.Property(x => x.LeftAnkleCm).HasPrecision(5, 2).IsRequired(false);
        builder.Property(x => x.RightAnkleCm).HasPrecision(5, 2).IsRequired(false);
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
        // MỘT giá trị chính thức cho mỗi (user, ngày, phương pháp): nhập lại cùng ngày thay thế giá trị cũ, hai request đồng thời không tạo hai dòng. Dòng đã xoá mềm không chặn tạo lại.
        // Hàng Tape là MỘT hàng rộng/ngày (mỗi số đo là một cột) nên số đo khác cùng ngày không bị ảnh hưởng; các Method khác nhau cùng ngày là các dòng riêng. Cần dọn dữ liệu trùng TRƯỚC khi áp dụng migration (xem Docs/Design/Verification/BodyMetric_UniqueDay_Migration_Proposal.md).
        builder.HasIndex(x => new { x.UserId, x.MeasuredDate, x.Method }).IsUnique().HasFilter("[IsDeleted] = 0").HasDatabaseName("UX_BodyMetrics_UserId_MeasuredDate_Method");
    }
}