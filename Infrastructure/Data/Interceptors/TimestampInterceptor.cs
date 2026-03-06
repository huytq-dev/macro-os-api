namespace Infrastructure;

public class TimestampInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
            UpdateTimestamps(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        if (eventData.Context is not null)
            UpdateTimestamps(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    private static void UpdateTimestamps(DbContext context)
    {
        var now = DateTimeOffset.UtcNow;

        var entries = context.ChangeTracker.Entries()
            .Where(e => e.Entity is IEntity &&
                        (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            var entity = (IEntity)entry.Entity;

            if (entry.State == EntityState.Added)
            {
                entity.CreatedAt = now;
                entity.ModifiedAt = null;

                if (entity is ISoftDeletable softEntity)
                {
                    softEntity.IsDeleted = false;
                    softEntity.DeletedAt = null;
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                entity.ModifiedAt = now;
                entry.Property(nameof(IEntity.CreatedAt)).IsModified = false;
            }
        }

        var softDeleteEntries = context.ChangeTracker.Entries()
            .Where(e => e.Entity is ISoftDeletable && e.State == EntityState.Deleted);

        foreach (var entry in softDeleteEntries)
        {
            entry.State = EntityState.Modified;
            var entity = (ISoftDeletable)entry.Entity;
            entity.IsDeleted = true;
            entity.DeletedAt = now;
        }
    }

}