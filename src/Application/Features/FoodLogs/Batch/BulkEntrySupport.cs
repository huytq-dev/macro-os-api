namespace Application;

/// <summary>Tải các entry của user theo id (tracking) cùng toàn bộ entries của log chứa chúng để tính lại tổng; id không phải của user bị bỏ qua.</summary>
internal static class BulkEntrySupport
{
    public const int MaxIds = 200;

    public static async Task<(List<FoodLogEntry> Selected, List<FoodLog> Logs)> LoadOwnedAsync(IUnitOfWork uow, Guid userId, IReadOnlyCollection<Guid> ids, CancellationToken ct)
    {
        var logIds = await uow.Repository<FoodLogEntry>().Query()
            .Where(e => ids.Contains(e.Id) && e.FoodLog.UserId == userId)
            .Select(e => e.FoodLogId).Distinct()
            .ToListAsync(ct);
        if (logIds.Count == 0) return ([], []);

        var logs = await uow.Repository<FoodLog>().Query(tracking: true)
            .Include(l => l.Entries)
            .Where(l => logIds.Contains(l.Id))
            .ToListAsync(ct);
        var selected = logs.SelectMany(l => l.Entries).Where(e => !e.IsDeleted && ids.Contains(e.Id)).ToList();
        return (selected, logs);
    }

    public static void Ids<T>(AbstractValidator<T> v, Expression<Func<T, List<Guid>>> ids)
        => v.RuleFor(ids).NotEmpty().WithMessage(ValidationMessages.Required)
            .Must(l => l is null || l.Count <= MaxIds).WithMessage(ValidationMessages.MaxEntriesPerCall(MaxIds))
            .Must(l => l is null || l.All(i => i != Guid.Empty)).WithMessage(ValidationMessages.InvalidId);
}
