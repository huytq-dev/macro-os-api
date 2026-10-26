namespace Application;

public sealed class GetDailyProgressQueryHandler(IUserClock _clock, IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<GetDailyProgressQuery, IReadOnlyList<DailyProgressModel>>
{
    public async Task<IReadOnlyList<DailyProgressModel>> Handle(GetDailyProgressQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var today = (await _clock.GetAsync(ct)).Today;   // ngày local của user (provenance CURRENT_ACTIVE cho hôm nay)
        var to = request.To ?? today;
        var from = request.From ?? to.AddDays(-30);
        var days = await _uow.Repository<DailyProgressSnapshot>().Query()
            .Where(s => s.UserId == userId && s.SnapshotDate >= from && s.SnapshotDate <= to)
            .OrderBy(s => s.SnapshotDate)
            .ProjectToType<DailyProgressModel>()
            .ToListAsync(ct);

        // TDEE từng ngày (Energy Balance chế độ Expenditure) nằm ở ExpenditureSnapshot cùng ngày
        var tdee = await _uow.Repository<ExpenditureSnapshot>().Query()
            .Where(s => s.UserId == userId && s.SnapshotDate >= from && s.SnapshotDate <= to)
            .ToDictionaryAsync(s => s.SnapshotDate, s => (decimal?)s.EstimatedTdeeKcal, ct);

        // Target KHÔNG đọc từ cột snapshot (cũ sau check-in/đổi goal): tính theo tuần program hiệu lực từng ngày, cùng nguồn Food Log/Energy Balance/Nutrients/Check-in
        var programs = days.Count == 0 ? [] : await ProgramHistory.LoadAsync(_uow, userId, days[0].SnapshotDate, days[^1].SnapshotDate, ct);

        // Macro đã ăn = Effective Daily Nutrition tính KHI ĐỌC (cột snapshot không nullable nên không biểu diễn được "chưa biết"): manual thắng Food Log, nhịn ăn hợp lệ = 0,
        // macro chưa nhập (calories-only) hoặc ngày chưa log = null, KHÔNG phải 0 g. Calories/IsLogged vẫn từ snapshot do DerivedDataSync dựng.
        var effective = days.Count == 0 ? [] : await FoodLogSupport.LoadDaysAsync(_uow, userId, days[0].SnapshotDate, days[^1].SnapshotDate, ct, includeFasting: true);

        return days.Select(d =>
        {
            var (target, weekId) = DailyProgressTargets.On(programs, d.SnapshotDate);
            effective.TryGetValue(d.SnapshotDate, out var day);
            return d with
            {
                CaloriesTarget = target, MacroProgramWeekId = weekId, EstimatedTdeeKcal = tdee.GetValueOrDefault(d.SnapshotDate), TargetProvenance = ProgramHistory.ProvenanceOn(programs, d.SnapshotDate, today),
                ProteinConsumed = day?.Protein, CarbsConsumed = day?.Carbs, FatConsumed = day?.Fat,
            };
        }).ToList();
    }
}
