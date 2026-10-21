namespace Infrastructure;

public class WorkoutProgramConfiguration : SoftDeleteEntityConfiguration<WorkoutProgram, Guid>
{
    public override void Configure(EntityTypeBuilder<WorkoutProgram> builder)
    {
        base.Configure(builder);

        builder.ToTable("WorkoutPrograms");
        builder.Property(x => x.Notes).HasMaxLength(1000).IsRequired(false);

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Goal).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(x => x.CreationMode).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(x => x.Icon).HasMaxLength(50).IsRequired(false);
        builder.Property(x => x.Color).HasMaxLength(20).IsRequired(false);
        builder.Property(x => x.RowVersion).IsRowVersion();

        // Tối đa 1 program Active / user (ngoài ra không giới hạn số program lưu)
        builder.HasIndex(x => x.UserId).IsUnique().HasFilter("[Status] = 'Active' AND [IsDeleted] = 0");
        builder.HasIndex(x => new { x.UserId, x.Status });

        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);

        // CurrentRevisionId: FK vòng với Revisions, NoAction để tránh cycle
        builder.HasOne<WorkoutProgramRevision>().WithMany().HasForeignKey(x => x.CurrentRevisionId)
            .OnDelete(DeleteBehavior.NoAction).IsRequired(false);
        builder.HasMany(x => x.Revisions).WithOne(x => x.WorkoutProgram).HasForeignKey(x => x.WorkoutProgramId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
