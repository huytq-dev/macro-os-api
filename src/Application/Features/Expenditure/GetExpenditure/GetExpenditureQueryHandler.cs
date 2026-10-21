namespace Application;

public sealed class GetExpenditureQueryHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider, IOptions<AlgorithmOptions> _options)
    : IRequestHandler<GetExpenditureQuery, ExpenditureResponse>
{
    public async Task<ExpenditureResponse> Handle(GetExpenditureQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var to = request.To ?? FoodLogSupport.Today(_timeProvider);
        var from = request.From ?? to.AddDays(-90);

        var snapshots = await _uow.Repository<ExpenditureSnapshot>().Query()
            .Where(s => s.UserId == userId && s.SnapshotDate >= from && s.SnapshotDate <= to)
            .OrderBy(s => s.SnapshotDate)
            .ProjectToType<ExpenditurePointModel>()
            .ToListAsync(ct);

        // Flux Range tính khi đọc từ WindowDays (không lưu): cùng công thức mọi điểm, đổi núm FluxTrendErrorKg là dải đổi theo
        var opt = _options.Value;
        var points = snapshots.Select(p =>
        {
            var half = ExpenditureCalculator.FluxHalfWidth(p.WindowDays, opt.KcalPerKg, opt.FluxTrendErrorKg);
            return p with { FluxLowKcal = p.EstimatedTdeeKcal - half, FluxHighKcal = p.EstimatedTdeeKcal + half };
        }).ToList();

        var current = points.LastOrDefault();
        string? reason = null;
        if (current?.Status == nameof(ExpenditureStatus.Holding))
        {
            var d7 = current.SnapshotDate.AddDays(-6);
            var logs = (await FoodLogSupport.LoadDaysAsync(_uow, userId, d7, current.SnapshotDate, ct, includeFasting: true))
                .Keys.ToList();
            var weighed = await _uow.Repository<WeightEntry>().Query()
                .Where(x => x.UserId == userId && x.LogDate >= d7 && x.LogDate <= current.SnapshotDate)
                .Select(x => x.LogDate).ToListAsync(ct);
            var last7 = Enumerable.Range(0, 7).Select(i => d7.AddDays(i))
                .Select(d => new ExpenditureDay(d, logs.Contains(d) ? 0 : null, weighed.Contains(d) ? 0 : null)).ToList();
            reason = ExpenditureCalculator.HoldingReason(last7);
        }

        var startDate = await _uow.Repository<MacroProgram>().Query()
            .Where(p => p.UserId == userId && p.IsActive)
            .Select(p => (DateOnly?)p.ExpenditureStartDate)
            .FirstOrDefaultAsync(ct);

        return new ExpenditureResponse(current, reason, startDate, points);
    }
}
