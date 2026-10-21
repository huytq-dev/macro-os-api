namespace Infrastructure;

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
