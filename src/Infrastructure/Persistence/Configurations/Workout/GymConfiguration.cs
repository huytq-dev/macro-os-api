namespace Infrastructure;

public class EquipmentConfiguration : BaseEntityConfiguration<Equipment, Guid>
{
    public override void Configure(EntityTypeBuilder<Equipment> builder)
    {
        base.Configure(builder);

        builder.ToTable("Equipment");

        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.DefaultIncrementKg).HasPrecision(5, 2).IsRequired(false);

        builder.HasIndex(x => x.Code).IsUnique();
    }
}

public class ExerciseAliasConfiguration : BaseEntityConfiguration<ExerciseAlias, Guid>
{
    public override void Configure(EntityTypeBuilder<ExerciseAlias> builder)
    {
        base.Configure(builder);

        builder.ToTable("ExerciseAliases");

        builder.Property(x => x.Locale).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Alias).HasMaxLength(256).IsRequired();
        builder.Property(x => x.AliasNormalized).HasMaxLength(256).IsRequired();

        builder.HasIndex(x => new { x.Locale, x.AliasNormalized });

        builder.HasOne(x => x.Exercise).WithMany().HasForeignKey(x => x.ExerciseId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ExerciseEquipmentConfiguration : BaseEntityConfiguration<ExerciseEquipment, Guid>
{
    public override void Configure(EntityTypeBuilder<ExerciseEquipment> builder)
    {
        base.Configure(builder);

        builder.ToTable("ExerciseEquipment");

        builder.Property(x => x.Requirement).HasConversion<string>().HasMaxLength(30).IsRequired();

        builder.HasIndex(x => new { x.ExerciseId, x.EquipmentId }).IsUnique();

        builder.HasOne(x => x.Exercise).WithMany().HasForeignKey(x => x.ExerciseId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Equipment).WithMany().HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class GymProfileConfiguration : SoftDeleteEntityConfiguration<GymProfile, Guid>
{
    public override void Configure(EntityTypeBuilder<GymProfile> builder)
    {
        base.Configure(builder);

        builder.ToTable("GymProfiles");
        builder.Property(x => x.LocationLabel).HasMaxLength(100).IsRequired(false);

        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(500).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();

        // Tối đa 1 gym mặc định / user
        builder.HasIndex(x => x.UserId).IsUnique().HasFilter("[IsDefault] = 1 AND [IsDeleted] = 0");

        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class GymEquipmentConfiguration : BaseEntityConfiguration<GymEquipment, Guid>
{
    public override void Configure(EntityTypeBuilder<GymEquipment> builder)
    {
        base.Configure(builder);

        builder.ToTable("GymEquipment", t => t.HasCheckConstraint(
            "CK_GymEquipment_LoadsNonNegative",
            "([IncrementKg] IS NULL OR [IncrementKg] > 0) AND ([MinLoadKg] IS NULL OR [MinLoadKg] >= 0) AND ([MaxLoadKg] IS NULL OR [MaxLoadKg] >= 0)"));

        builder.Property(x => x.IncrementKg).HasPrecision(5, 2).IsRequired(false);
        builder.Property(x => x.MinLoadKg).HasPrecision(6, 2).IsRequired(false);
        builder.Property(x => x.MaxLoadKg).HasPrecision(6, 2).IsRequired(false);

        builder.HasIndex(x => new { x.GymProfileId, x.EquipmentId }).IsUnique();

        builder.HasOne(x => x.GymProfile).WithMany(x => x.Equipment).HasForeignKey(x => x.GymProfileId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Equipment).WithMany().HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.Restrict);
    }
}

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
