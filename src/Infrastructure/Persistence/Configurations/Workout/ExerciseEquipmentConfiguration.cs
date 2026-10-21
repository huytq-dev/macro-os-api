namespace Infrastructure;

public class ExerciseEquipmentConfiguration : BaseEntityConfiguration<ExerciseEquipment, Guid>
{
    public override void Configure(EntityTypeBuilder<ExerciseEquipment> builder)
    {
        base.Configure(builder);

        builder.ToTable("ExerciseEquipment");

        builder.Property(x => x.Requirement).HasConversion<string>().HasMaxLength(30).IsRequired();

        builder.HasIndex(x => new { x.ExerciseId, x.EquipmentId }).IsUnique();

        builder.HasOne(x => x.Exercise).WithMany().HasForeignKey(x => x.ExerciseId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Equipment).WithMany().HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.Restrict);
    }
}
