namespace Infrastructure;

public class FoodLogNoteConfiguration : BaseEntityConfiguration<FoodLogNote, Guid>
{
    public override void Configure(EntityTypeBuilder<FoodLogNote> builder)
    {
        base.Configure(builder);

        builder.ToTable("FoodLogNotes");

        builder.Property(x => x.Name).HasMaxLength(FoodLogNoteLimits.NameMaxLength).IsRequired(false);
        builder.Property(x => x.Text).HasMaxLength(FoodLogNoteLimits.TextMaxLength).IsRequired();
        builder.Property(x => x.Color).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(x => x.NotedAt).IsRequired(false);

        builder.HasIndex(x => new { x.UserId, x.LogDate });

        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
