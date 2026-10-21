namespace Infrastructure;

public class CrossDomainRecommendationConfiguration : BaseEntityConfiguration<CrossDomainRecommendation, Guid>
{
    public override void Configure(EntityTypeBuilder<CrossDomainRecommendation> builder)
    {
        base.Configure(builder);

        builder.ToTable("CrossDomainRecommendations");

        builder.Property(x => x.Type).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.PayloadJson).IsRequired();

        builder.HasIndex(x => new { x.UserId, x.Status });

        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
