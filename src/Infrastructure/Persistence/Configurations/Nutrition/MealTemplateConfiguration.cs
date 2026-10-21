namespace Infrastructure;

public class MealTemplateConfiguration : SoftDeleteEntityConfiguration<MealTemplate, Guid>
{
    public override void Configure(EntityTypeBuilder<MealTemplate> builder)
    {
        base.Configure(builder);

        builder.ToTable("MealTemplates");

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();

        builder.HasIndex(x => new { x.UserId, x.Name });

        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Items).WithOne(x => x.MealTemplate).HasForeignKey(x => x.MealTemplateId).OnDelete(DeleteBehavior.Cascade);
    }
}
