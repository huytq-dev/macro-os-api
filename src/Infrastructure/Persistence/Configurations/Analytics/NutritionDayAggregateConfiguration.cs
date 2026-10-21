namespace Infrastructure;

public class NutritionDayAggregateConfiguration : BaseEntityConfiguration<NutritionDayAggregate, Guid>
{
    public override void Configure(EntityTypeBuilder<NutritionDayAggregate> builder)
    {
        base.Configure(builder);

        builder.ToTable("NutritionDayAggregates");

        builder.Property(x => x.Calories).HasPrecision(10, 2).IsRequired();
        builder.Property(x => x.Protein).HasPrecision(10, 2).IsRequired();
        builder.Property(x => x.Carbs).HasPrecision(10, 2).IsRequired();
        builder.Property(x => x.Fat).HasPrecision(10, 2).IsRequired();
        builder.Property(x => x.Fiber).HasPrecision(10, 2).IsRequired();
        builder.Property(x => x.NutrientsJson).IsRequired(false);
        builder.HasIndex(x => new { x.UserId, x.LocalDate }).IsUnique();

        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
