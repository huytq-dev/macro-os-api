namespace Application;

public sealed class GetExpenditureQueryHandler(IUserClock _clock, IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider, IOptions<AlgorithmOptions> _options)
    : IRequestHandler<GetExpenditureQuery, ExpenditureResponse>
{
    public async Task<ExpenditureResponse> Handle(GetExpenditureQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var to = request.To ?? (await _clock.GetAsync(ct)).Today;
        var from = request.From ?? to.AddDays(-90);

        var snapshots = await _uow.Repository<ExpenditureSnapshot>().Query()
            .Where(s => s.UserId == userId && s.SnapshotDate >= from && s.SnapshotDate <= to)
            .OrderBy(s => s.SnapshotDate)
            .ProjectToType<ExpenditurePointModel>()
            .ToListAsync(ct);

        var opt = _options.Value;
        var startDate = await _uow.Repository<MacroProgram>().Query()
            .Where(p => p.UserId == userId && p.IsActive)
            .Select(p => (DateOnly?)p.ExpenditureStartDate)
            .FirstOrDefaultAsync(ct);
        // Flux Range (user yêu cầu hiển thị, thay quyết định "giữ ẩn"): CÙNG điểm tính với series/insights (ExpenditureFluxService) trên chuỗi đã học tới `to`; null khi Calibrating hoặc chưa có quan sát hợp lệ
        var raw = await ExpenditureSeriesSupport.BuildAsync(_uow, userId, to, startDate, opt, ct);
        var bands = await ExpenditureFluxService.LoadBandsAsync(_uow, userId, raw, startDate, opt, ct);
        var points = snapshots.Select(p => p with
        {
            FluxLowKcal = bands.TryGetValue(p.SnapshotDate, out var band) ? band.LowerKcal : null,
            FluxHighKcal = band?.UpperKcal,
            IsCalibrating = startDate is { } s && ExpenditureCalculator.IsCalibrating(p.SnapshotDate, s, opt.CalibratingDays),
        }).ToList();

        var current = points.LastOrDefault();
        // Lý do chưa Updating cho Holding VÀ Calibrating (trước đây chỉ Holding và chỉ theo quy tắc 4/7 log + 1/7 cân, nên Holding do thiếu độ đổi cân nặng hiện reason = null)
        string? reason = null;
        if (current is { } c && c.Status != nameof(ExpenditureStatus.Updating))
        {
            var (statusDays, window) = await ExpenditureDiagnostics.LoadAsync(_uow, userId, c.SnapshotDate, startDate, opt, ct);
            int? left = opt.FreezeTdeeDuringCalibration && c.Status == nameof(ExpenditureStatus.Calibrating) && startDate is { } st ? Math.Max(0, opt.CalibratingDays - (c.SnapshotDate.DayNumber - st.DayNumber)) : null;
            reason = ExpenditureDiagnostics.Diagnose(statusDays, window, c.SnapshotDate, opt.ExpenditureWindowDays, left).Reason;
        }

        return new ExpenditureResponse(current, reason, startDate, points, points.Any(p => p.FluxLowKcal is not null) ? FluxCalculationPolicy.V1Version : null);
    }
}
