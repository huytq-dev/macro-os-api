namespace Application;

public static class ExpenditureSeriesSupport
{
    /// <summary>Snapshot đã lưu + ExpenditureStartDate của program đang chạy; chuỗi liên tục đến <paramref name="today"/>.</summary>
    public static async Task<(List<ExpenditureSeriesPoint> Raw, DateOnly? Start)> LoadAsync(IUnitOfWork uow, Guid userId, DateOnly today, AlgorithmOptions opt, CancellationToken ct)
    {
        var start = await uow.Repository<MacroProgram>().Query()
            .Where(p => p.UserId == userId && p.IsActive)
            .Select(p => (DateOnly?)p.ExpenditureStartDate)
            .FirstOrDefaultAsync(ct);
        return (await BuildAsync(uow, userId, today, start, opt, ct), start);
    }

    /// <summary>Như <see cref="LoadAsync"/> khi caller đã có ExpenditureStartDate (đã tải program): một truy vấn snapshot.</summary>
    public static async Task<List<ExpenditureSeriesPoint>> BuildAsync(IUnitOfWork uow, Guid userId, DateOnly today, DateOnly? start, AlgorithmOptions opt, CancellationToken ct)
    {
        var rows = (await uow.Repository<ExpenditureSnapshot>().Query()
            .Where(s => s.UserId == userId && s.SnapshotDate <= today)
            .Select(s => new { s.SnapshotDate, s.EstimatedTdeeKcal, s.Status, s.WindowDays })
            .ToListAsync(ct)).Select(s => new ExpenditureSnapshotRow(s.SnapshotDate, s.EstimatedTdeeKcal, s.Status.ToString(), s.WindowDays));
        return ExpenditureSeries.Daily(rows, today, start, opt.CalibratingDays, opt.FreezeTdeeDuringCalibration);
    }
}
