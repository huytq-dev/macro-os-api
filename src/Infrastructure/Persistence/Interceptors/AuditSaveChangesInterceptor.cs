namespace Infrastructure;

/// <summary>
/// Tự gán CreatedAt/ModifiedAt cho mọi IEntity, CreatedBy/UpdatedBy cho IAuditable,
/// và đổi Delete thành soft delete cho ISoftDeletable. Không gán tay các cột này trong code nghiệp vụ.
/// </summary>
[RegisterService(typeof(AuditSaveChangesInterceptor), ServiceLifetime.Singleton)]
public sealed class AuditSaveChangesInterceptor(IHttpContextAccessor httpContextAccessor, TimeProvider timeProvider)
    : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateAuditEntities(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken ct = default)
    {
        UpdateAuditEntities(eventData.Context);
        return base.SavingChangesAsync(eventData, result, ct);
    }

    private void UpdateAuditEntities(DbContext? context)
    {
        if (context is null) return;

        var now = timeProvider.GetUtcNow();
        var userContext = httpContextAccessor.HttpContext?.RequestServices.GetService<IUserContext>();
        var userId = userContext is { IsAuthenticated: true } ? userContext.UserId : (Guid?)null;

        foreach (var entry in context.ChangeTracker.Entries().ToList())
        {
            switch (entry.State)
            {
                case EntityState.Added when entry.Entity is IEntity added:
                    added.CreatedAt = now;
                    added.ModifiedAt = null;
                    if (added is IAuditable a) a.CreatedBy = userId;
                    if (added is ISoftDeletable s) { s.IsDeleted = false; s.DeletedAt = null; }
                    break;

                case EntityState.Modified when entry.Entity is IEntity modified:
                    modified.ModifiedAt = now;
                    if (modified is IAuditable m) m.UpdatedBy = userId;
                    entry.Property(nameof(IEntity.CreatedAt)).IsModified = false;
                    break;

                case EntityState.Deleted when entry.Entity is ISoftDeletable soft:
                    entry.State = EntityState.Modified;
                    soft.IsDeleted = true;
                    soft.DeletedAt = now;
                    break;
            }
        }
    }
}
