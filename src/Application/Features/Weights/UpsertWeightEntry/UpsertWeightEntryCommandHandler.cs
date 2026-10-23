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
        var locale = await UserLocale.GetAsync(_uow, userId, _timeProvider, ct);
        var logDate = request.LogDate ?? locale.Today;
        locale.EnsureNotFuture(logDate, "LogDate");

        // Một lần cân / ngày (unique (UserId, LogDate)): nhập lại cùng ngày CẬP NHẬT dòng đó. Hai request đồng thời tạo dòng đầu ⇒ bên thua chạy lại như cập nhật dòng của bên thắng (IdempotentWrite), không lỗi 500
        var (entryId, written) = await IdempotentWrite.RunAsync(async () =>
        {
            // MỘT lần đọc: mọi dòng cân của user (kể cả row đã soft-delete: unique (UserId, LogDate) vẫn giữ row đó nên phải hồi sinh thay vì insert), tracked
            var all = await _uow.Repository<WeightEntry>().Query(tracking: true).IgnoreQueryFilters().Where(w => w.UserId == userId).ToListAsync(ct);
            var entry = all.FirstOrDefault(w => w.LogDate == logDate);
            if (request.CreateOnly && entry is { IsDeleted: false }) return (entry.Id, false);   // đã có lần cân của ngày đó: không sửa gì
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

            // % mỡ cân của cùng ngày (tuỳ chọn): ghi/ghi đè một dòng Scale, hoặc xoá khi ClearBodyFat; lưu cùng lần với cân nặng
            if (request.BodyFatPercent is not null || request.ClearBodyFat)
            {
                var repo = _uow.Repository<BodyMetric>();
                var rows = await repo.Query(tracking: true)
                    .Where(b => b.UserId == userId && b.MeasuredDate == logDate && (b.Method == MeasurementMethod.Scale || b.Method == MeasurementMethod.SmartScale) && b.BodyFatPercent != null)
                    .OrderByDescending(b => b.CreatedAt)
                    .ToListAsync(ct);
                if (request.BodyFatPercent is { } pct)
                {
                    var row = rows.FirstOrDefault();
                    if (row is null)
                    {
                        row = new BodyMetric { Id = Guid.NewGuid(), UserId = userId, MeasuredDate = logDate, Method = MeasurementMethod.Scale };
                        await repo.AddAsync(row, ct);
                    }
                    row.BodyFatPercent = pct;
                }
                else
                    foreach (var row in rows) await repo.RemoveAsync(row, ct); // interceptor chuyển thành soft delete
            }

            // Trend tính lại cho mọi dòng chưa xoá (cùng kết quả WeightTrendRefresher) rồi lưu MỘT lần
            WeightTrendRefresher.Apply(all.Where(w => !w.IsDeleted).ToList(), _options.Value.TrendDays);
            await _uow.SaveChangesAsync(ct);
            return (entry.Id, true);
        }, _uow.DiscardChanges);

        // Trend đổi từ logDate → expenditure tính lại từ đó (UC-F03): sau khi trả phản hồi nếu có chạy nền
        if (written) await WeightTrendRefresher.RebuildExpenditureAsync(_uow, userId, logDate, _options.Value, _timeProvider, ct);

        return entryId;
    }
}
