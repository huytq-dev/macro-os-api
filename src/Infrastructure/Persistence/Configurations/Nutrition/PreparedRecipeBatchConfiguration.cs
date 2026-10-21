namespace Infrastructure;

public class PreparedRecipeBatchConfiguration : BaseEntityConfiguration<PreparedRecipeBatch, Guid>
{
    public override void Configure(EntityTypeBuilder<PreparedRecipeBatch> builder)
    {
        base.Configure(builder);

        builder.ToTable("PreparedRecipeBatches", t => t.HasCheckConstraint(
            "CK_PreparedRecipeBatches_YieldPositive", "[ActualYieldGrams] > 0"));

        builder.Property(x => x.ActualYieldGrams).HasPrecision(10, 2).IsRequired();

        builder.HasIndex(x => new { x.UserId, x.PreparedAt });

        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.RecipeVersion).WithMany().HasForeignKey(x => x.RecipeVersionId).OnDelete(DeleteBehavior.Restrict);
    }
}
