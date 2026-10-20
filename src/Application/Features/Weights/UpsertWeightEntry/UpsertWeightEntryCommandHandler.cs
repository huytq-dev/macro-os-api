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

        // MỘT lần đọc: mọi dòng cân của user (kể cả row đã soft-delete: unique (UserId, LogDate) vẫn giữ row đó nên phải hồi sinh thay vì insert), tracked
        var all = await _uow.Repository<WeightEntry>().Query(tracking: true).IgnoreQueryFilters().Where(w => w.UserId == userId).ToListAsync(ct);
        var entry = all.FirstOrDefault(w => w.LogDate == logDate);
        if (entry is null)
        {
            entry = new WeightEntry { Id = Guid.NewGuid(), UserId = userId, LogDate = logDate };
            await _uow.Repository<WeightEntry>().AddAsync(entry, ct);
            all.Add(entry);
        }

        entry.WeightKg = request.WeightKg;
        entry.Notes = request.Notes;
        entry.IsDeleted = false;
        entry.DeletedAt = null;

        // Trend tính lại cho mọi dòng chưa xoá (cùng kết quả WeightTrendRefresher) rồi lưu MỘT lần
        WeightTrendRefresher.Apply(all.Where(w => !w.IsDeleted).ToList(), _options.Value.TrendDays);
        await _uow.SaveChangesAsync(ct);

        // Trend đổi từ logDate → expenditure tính lại từ đó (UC-F03): sau khi trả phản hồi nếu có chạy nền
        await WeightTrendRefresher.RebuildExpenditureAsync(_uow, userId, logDate, _options.Value, _timeProvider, ct);

        return entry.Id;
    }
}
