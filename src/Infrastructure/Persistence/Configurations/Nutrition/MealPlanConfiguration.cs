namespace Infrastructure;

public class MealPlanConfiguration : SoftDeleteEntityConfiguration<MealPlan, Guid>
{
    public override void Configure(EntityTypeBuilder<MealPlan> builder)
    {
        base.Configure(builder);

        builder.ToTable("MealPlans", t => t.HasCheckConstraint("CK_MealPlans_Dates", "[EndDate] >= [StartDate]"));

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();

        builder.HasIndex(x => new { x.UserId, x.StartDate });

        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Meals).WithOne(x => x.MealPlan).HasForeignKey(x => x.MealPlanId).OnDelete(DeleteBehavior.Cascade);
    }
}
