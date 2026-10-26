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

        builder.Property(x => x.IsIncomplete).HasDefaultValue(false);
        builder.Property(x => x.IsFasting).HasDefaultValue(false);

        // Denormalized daily totals
        builder.Property(x => x.TotalCalories).HasPrecision(8, 2).IsRequired();
        builder.Property(x => x.TotalProtein).HasPrecision(8, 2).IsRequired();
        builder.Property(x => x.TotalCarbs).HasPrecision(8, 2).IsRequired();
        builder.Property(x => x.TotalFat).HasPrecision(8, 2).IsRequired();
        builder.Property(x => x.TotalFiber).HasPrecision(8, 2).IsRequired();
        builder.Property(x => x.TotalWater).HasPrecision(8, 2).IsRequired();

        // Manual Nutrition (S29): nullable, không default, không backfill; cùng độ chính xác với tổng ngày. Biên khớp validator của NDM (kcal 0–10000, macro 0–1000)
        builder.Property(x => x.ManualCaloriesKcal).HasPrecision(8, 2);
        builder.Property(x => x.ManualProteinG).HasPrecision(8, 2);
        builder.Property(x => x.ManualCarbsG).HasPrecision(8, 2);
        builder.Property(x => x.ManualFatG).HasPrecision(8, 2);
        builder.ToTable(t => t.HasCheckConstraint("CK_FoodLogs_ManualNutrition_Range",
            "([ManualCaloriesKcal] IS NULL OR ([ManualCaloriesKcal] >= 0 AND [ManualCaloriesKcal] <= 10000)) " +
            "AND ([ManualProteinG] IS NULL OR ([ManualProteinG] >= 0 AND [ManualProteinG] <= 1000)) " +
            "AND ([ManualCarbsG] IS NULL OR ([ManualCarbsG] >= 0 AND [ManualCarbsG] <= 1000)) " +
            "AND ([ManualFatG] IS NULL OR ([ManualFatG] >= 0 AND [ManualFatG] <= 1000))"));

        builder.Property(x => x.RowVersion).IsRowVersion();

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
