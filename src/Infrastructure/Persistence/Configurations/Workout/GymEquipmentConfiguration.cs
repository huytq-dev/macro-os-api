namespace Infrastructure;

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
