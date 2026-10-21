namespace Infrastructure;

public class UserFoodIconConfiguration : BaseEntityConfiguration<UserFoodIcon, Guid>
{
    public override void Configure(EntityTypeBuilder<UserFoodIcon> builder)
    {
        base.Configure(builder);

        builder.ToTable("UserFoodIcons");

        builder.Property(x => x.IconKey).HasMaxLength(64).IsRequired();
        // Mỗi user chỉ có một icon riêng cho một món
        builder.HasIndex(x => new { x.UserId, x.FoodItemId }).IsUnique();

        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.FoodItem).WithMany().HasForeignKey(x => x.FoodItemId).OnDelete(DeleteBehavior.Restrict);
    }
}
