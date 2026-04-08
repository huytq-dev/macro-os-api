namespace Infrastructure;

public class FoodLogConfiguration : BaseEntityConfiguration<FoodLog, Guid>
{
    public override void Configure(EntityTypeBuilder<FoodLog> builder)
    {
        base.Configure(builder);

        builder.ToTable("FoodLogs");

        // --- Properties ---
        builder.Property(x => x.Notes)
            .HasMaxLength(500)
            .IsRequired(false);

        // Denormalized daily totals
        builder.Property(x => x.TotalCalories).HasPrecision(8, 2).IsRequired();
        builder.Property(x => x.TotalProtein).HasPrecision(8, 2).IsRequired();
        builder.Property(x => x.TotalCarbs).HasPrecision(8, 2).IsRequired();
        builder.Property(x => x.TotalFat).HasPrecision(8, 2).IsRequired();
        builder.Property(x => x.TotalFiber).HasPrecision(8, 2).IsRequired();
        builder.Property(x => x.TotalWater).HasPrecision(8, 2).IsRequired();

        // --- Indexes ---
        // Unique: 1 log per user per day
        builder.HasIndex(x => new { x.UserId, x.LogDate })
            .IsUnique();

        // --- Relationships ---
        builder.HasMany(x => x.Entries)
            .WithOne(x => x.FoodLog)
            .HasForeignKey(x => x.FoodLogId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.WaterLogs)
            .WithOne(x => x.FoodLog)
            .HasForeignKey(x => x.FoodLogId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class FoodLogEntryConfiguration : SoftDeleteEntityConfiguration<FoodLogEntry, Guid>
{
    public override void Configure(EntityTypeBuilder<FoodLogEntry> builder)
    {
        base.Configure(builder);

        builder.ToTable("FoodLogEntries");

        // --- Properties ---
        builder.Property(x => x.MealType)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.EatenAt).IsRequired(false);
        builder.Property(x => x.FoodItemId).IsRequired(false);
        builder.Property(x => x.RecipeId).IsRequired(false);
        builder.Property(x => x.FoodServingId).IsRequired(false);
        builder.Property(x => x.Quantity).HasPrecision(8, 2).IsRequired();

        // Nutrition snapshot
        builder.Property(x => x.Calories).HasPrecision(8, 2).IsRequired();
        builder.Property(x => x.Protein).HasPrecision(8, 2).IsRequired();
        builder.Property(x => x.Carbs).HasPrecision(8, 2).IsRequired();
        builder.Property(x => x.Fat).HasPrecision(8, 2).IsRequired();
        builder.Property(x => x.Fiber).HasPrecision(8, 2).IsRequired(false);
        builder.Property(x => x.Sodium).HasPrecision(8, 2).IsRequired(false);

        // --- Constraints ---
        // FoodItem XOR Recipe — không được cả hai hoặc không có cái nào
        // Khi log Recipe, FoodServingId phải NULL (serving thuộc FoodItem, không áp dụng cho Recipe)
        builder.ToTable(t => t.HasCheckConstraint(
            "CK_FoodLogEntries_FoodOrRecipe",
            "([FoodItemId] IS NOT NULL AND [RecipeId] IS NULL) OR " +
            "([FoodItemId] IS NULL AND [RecipeId] IS NOT NULL AND [FoodServingId] IS NULL)"
        ));

        // --- Indexes ---
        builder.HasIndex(x => new { x.FoodLogId, x.MealType });

        // --- Relationships ---
        builder.HasOne(x => x.FoodItem)
            .WithMany(x => x.LogEntries)
            .HasForeignKey(x => x.FoodItemId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasOne(x => x.Recipe)
            .WithMany(x => x.LogEntries)
            .HasForeignKey(x => x.RecipeId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasOne(x => x.FoodServing)
            .WithMany(x => x.LogEntries)
            .HasForeignKey(x => x.FoodServingId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        builder.HasOne(x => x.Micros)
            .WithOne(x => x.FoodLogEntry)
            .HasForeignKey<FoodLogEntryMicro>(x => x.FoodLogEntryId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);
    }
}

public class WaterLogConfiguration : BaseEntityConfiguration<WaterLog, Guid>
{
    public override void Configure(EntityTypeBuilder<WaterLog> builder)
    {
        base.Configure(builder);

        builder.ToTable("WaterLogs");

        builder.Property(x => x.AmountMl).HasPrecision(8, 2).IsRequired();
        builder.Property(x => x.LoggedAt).IsRequired();
        builder.Property(x => x.Source).HasMaxLength(50).IsRequired(false);

        // --- Indexes ---
        builder.HasIndex(x => new { x.UserId, x.LoggedAt });
    }
}
public class FoodLogEntryMicroConfiguration : BaseEntityConfiguration<FoodLogEntryMicro, Guid>
{
    public override void Configure(EntityTypeBuilder<FoodLogEntryMicro> builder)
    {
        base.Configure(builder);

        builder.ToTable("FoodLogEntryMicros");

        builder.Property(x => x.Sugar).HasPrecision(8, 2).IsRequired(false);
        builder.Property(x => x.SaturatedFat).HasPrecision(8, 2).IsRequired(false);
        builder.Property(x => x.TransFat).HasPrecision(8, 2).IsRequired(false);
        builder.Property(x => x.Cholesterol).HasPrecision(8, 2).IsRequired(false);
        builder.Property(x => x.Potassium).HasPrecision(8, 2).IsRequired(false);
        builder.Property(x => x.VitaminA).HasPrecision(8, 2).IsRequired(false);
        builder.Property(x => x.VitaminC).HasPrecision(8, 2).IsRequired(false);
        builder.Property(x => x.VitaminD).HasPrecision(8, 2).IsRequired(false);
        builder.Property(x => x.Calcium).HasPrecision(8, 2).IsRequired(false);
        builder.Property(x => x.Iron).HasPrecision(8, 2).IsRequired(false);

        // --- Indexes ---
        // Unique: 1:1 với FoodLogEntry
        builder.HasIndex(x => x.FoodLogEntryId).IsUnique();
    }
}
