namespace Infrastructure;

public class UserShortcutSettingConfiguration : BaseEntityConfiguration<UserShortcutSetting, Guid>
{
    public override void Configure(EntityTypeBuilder<UserShortcutSetting> builder)
    {
        base.Configure(builder);

        builder.ToTable("UserShortcutSettings");

        builder.Property(x => x.ToolbarStyle).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.ShortcutsJson).HasMaxLength(2000).IsRequired();

        // 1 dòng/user
        builder.HasIndex(x => x.UserId).IsUnique();

        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
