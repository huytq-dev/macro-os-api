namespace Infrastructure;

public class FoodFavoriteConfiguration : BaseEntityConfiguration<FoodFavorite, Guid>
{
    public override void Configure(EntityTypeBuilder<FoodFavorite> builder)
    {
        base.Configure(builder);

        builder.ToTable("FoodFavorites");

        builder.Property(x => x.Quantity).HasPrecision(10, 2);
        // Một preset = (món, khẩu phần, số lượng); cùng món được nhiều preset khác nhau.
        builder.HasIndex(x => new { x.UserId, x.FoodItemId, x.FoodServingId, x.Quantity }).IsUnique();
        builder.HasIndex(x => new { x.UserId, x.SortOrder });

        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.FoodItem).WithMany().HasForeignKey(x => x.FoodItemId).OnDelete(DeleteBehavior.Restrict);
        // UpdateCustomFood thay toàn bộ serving: preset dùng serving đã mất bị xoá theo (không giữ preset trỏ vào khẩu phần không còn).
        builder.HasOne(x => x.FoodServing).WithMany().HasForeignKey(x => x.FoodServingId).OnDelete(DeleteBehavior.Cascade);
    }
}
