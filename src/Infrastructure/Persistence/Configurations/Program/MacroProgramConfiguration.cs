namespace Infrastructure;

public class MacroProgramConfiguration : SoftDeleteEntityConfiguration<MacroProgram, Guid>
{
    public override void Configure(EntityTypeBuilder<MacroProgram> builder)
    {
        base.Configure(builder);

        builder.ToTable("MacroPrograms");
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired(false);
        builder.Property(x => x.Description).HasMaxLength(500).IsRequired(false);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.Source).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.StrategyVersion).HasMaxLength(20).IsRequired(false);
        builder.Property(x => x.TargetWeightKg).HasPrecision(5, 2).IsRequired(false);
        builder.Property(x => x.CalorieShiftMode).HasConversion<string>().HasMaxLength(30).HasDefaultValue(CalorieShiftMode.Even).IsRequired();
        builder.Property(x => x.CalorieShiftPercent).HasPrecision(5, 2).IsRequired(false);
        builder.Property(x => x.CalorieShiftDays).HasDefaultValue(DaysOfWeek.None);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasOne<Goal>().WithMany().HasForeignKey(x => x.GoalId).OnDelete(DeleteBehavior.Restrict).IsRequired(false);

        // --- Properties ---
        builder.Property(x => x.ProgramType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.GoalType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.DietStyle)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.WeeklyWeightChangeTargetKg)
            .HasPrecision(4, 2)
            .IsRequired();

        builder.Property(x => x.WeeklyWeightChangeTargetPercent)
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(x => x.ProteinLevel)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.CalorieFloorKcal).IsRequired(false);

        builder.Property(x => x.CheckInWeekday)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.ExpenditureStartDate).IsRequired();
        builder.Property(x => x.ManualInitialTdeeKcal).IsRequired(false);

        builder.Property(x => x.ProteinGPerKgBodyweight)
            .HasPrecision(4, 2)
            .IsRequired();

        builder.Property(x => x.FixedProteinGrams)
            .HasPrecision(6, 2)
            .IsRequired(false);

        builder.Property(x => x.StartDate).IsRequired();
        builder.Property(x => x.IsActive).HasDefaultValue(true);

        // --- Indexes ---
        // Partial unique: chỉ 1 active program/user
        builder.HasIndex(x => x.UserId)
            .IsUnique()
            .HasFilter("[IsActive] = 1 AND [IsDeleted] = 0");

        // --- Relationships ---
        builder.HasMany(x => x.Weeks)
            .WithOne(x => x.MacroProgram)
            .HasForeignKey(x => x.MacroProgramId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.DayTargets)
            .WithOne(x => x.MacroProgram)
            .HasForeignKey(x => x.MacroProgramId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class MacroProgramWeekConfiguration : BaseEntityConfiguration<MacroProgramWeek, Guid>
{
    public override void Configure(EntityTypeBuilder<MacroProgramWeek> builder)
    {
        base.Configure(builder);

        builder.ToTable("MacroProgramWeeks");
        builder.Property(x => x.RevisionNo);
        builder.HasIndex(x => new { x.MacroProgramId, x.RevisionNo }).IsUnique();
        builder.Property(x => x.AlgorithmVersion).HasMaxLength(20).IsRequired(false);
        builder.Property(x => x.PreferencesJson).HasMaxLength(2000).IsRequired(false);

        // --- Properties ---
        builder.Property(x => x.CaloriesTarget).IsRequired();
        builder.Property(x => x.ProteinTarget).HasPrecision(6, 2).IsRequired();
        builder.Property(x => x.CarbsTarget).HasPrecision(6, 2).IsRequired();
        builder.Property(x => x.FatTarget).HasPrecision(6, 2).IsRequired();

        builder.Property(x => x.AdjustmentReason)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.PreviousCaloriesTarget).IsRequired(false);

        // --- Indexes ---
        // Unique: 1 target row per program per week start date
        builder.HasIndex(x => new { x.MacroProgramId, x.EffectiveFrom }).IsUnique();
    }
}

public class MacroProgramDayTargetConfiguration : BaseEntityConfiguration<MacroProgramDayTarget, Guid>
{
    public override void Configure(EntityTypeBuilder<MacroProgramDayTarget> builder)
    {
        base.Configure(builder);

        builder.ToTable("MacroProgramDayTargets");
        builder.Property(x => x.DayType).HasConversion<string>().HasMaxLength(30).HasDefaultValue(DayType.Standard).IsRequired();
        builder.Property(x => x.AdditionalTargetsJson).HasMaxLength(1000).IsRequired(false);

        builder.Property(x => x.DayOfWeek)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(x => x.IsFastingDay).HasDefaultValue(false);
        builder.Property(x => x.IsLocked).HasDefaultValue(false);

        builder.Property(x => x.CaloriesTarget).IsRequired(false);
        builder.Property(x => x.ProteinTarget).HasPrecision(6, 2).IsRequired(false);
        builder.Property(x => x.CarbsTarget).HasPrecision(6, 2).IsRequired(false);
        builder.Property(x => x.FatTarget).HasPrecision(6, 2).IsRequired(false);

        // --- Indexes ---
        // Unique: 1 day config per program per day of week
        builder.HasIndex(x => new { x.MacroProgramId, x.DayOfWeek }).IsUnique();
    }
}
