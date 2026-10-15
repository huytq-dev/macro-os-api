namespace Application;

public sealed class UpsertWeightEntryCommandHandler(
    IUnitOfWork _uow,
    IUserContext _userContext,
    IOptions<AlgorithmOptions> _options,
    TimeProvider _timeProvider
) : IRequestHandler<UpsertWeightEntryCommand, Guid>
{
    public async Task<Guid> Handle(UpsertWeightEntryCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var logDate = request.LogDate ?? DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);

        // Kể cả row đã soft-delete: unique (UserId, LogDate) vẫn giữ row đó nên phải hồi sinh thay vì insert
        var entry = await _uow.Repository<WeightEntry>().Query(tracking: true)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(w => w.UserId == userId && w.LogDate == logDate, ct);

        if (entry is null)
        {
            entry = new WeightEntry { Id = Guid.NewGuid(), UserId = userId, LogDate = logDate };
            await _uow.Repository<WeightEntry>().AddAsync(entry, ct);
        }

        entry.WeightKg = request.WeightKg;
        entry.Notes = request.Notes;
        entry.IsDeleted = false;
        entry.DeletedAt = null;

        await _uow.SaveChangesAsync(ct); // để GetAllTrackedAsync thấy entry mới/hồi sinh
        await WeightTrendRefresher.RefreshAsync(_uow, userId, _options.Value.TrendDays, ct);
        await _uow.SaveChangesAsync(ct);

        return entry.Id;
    }
}
