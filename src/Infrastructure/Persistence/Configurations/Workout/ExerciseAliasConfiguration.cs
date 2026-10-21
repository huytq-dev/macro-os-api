namespace Infrastructure;

public class ExerciseAliasConfiguration : BaseEntityConfiguration<ExerciseAlias, Guid>
{
    public override void Configure(EntityTypeBuilder<ExerciseAlias> builder)
    {
        base.Configure(builder);

        builder.ToTable("ExerciseAliases");

        builder.Property(x => x.Locale).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Alias).HasMaxLength(256).IsRequired();
        builder.Property(x => x.AliasNormalized).HasMaxLength(256).IsRequired();

        builder.HasIndex(x => new { x.Locale, x.AliasNormalized });

        builder.HasOne(x => x.Exercise).WithMany().HasForeignKey(x => x.ExerciseId).OnDelete(DeleteBehavior.Cascade);
    }
}
