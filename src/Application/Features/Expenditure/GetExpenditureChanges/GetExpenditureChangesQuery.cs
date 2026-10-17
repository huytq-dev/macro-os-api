namespace Application;

/// <summary>
/// UC-F05: biến thiên TDEE hiện tại so với 3/7/14/30/90 ngày trước. Waiting (DeltaKcal null) khi chưa có điểm ở mốc đó
/// (mốc trước ExpenditureStartDate hoặc chưa đủ lịch sử). BE tính vì FE chỉ giữ điểm của kỳ đang xem (1W không đủ 90 ngày).
/// </summary>
public sealed record GetExpenditureChangesQuery : IRequest<IReadOnlyList<ExpenditureChangeModel>>;

public sealed class GetExpenditureChangesQueryHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<GetExpenditureChangesQuery, IReadOnlyList<ExpenditureChangeModel>>
{
    public static readonly int[] Windows = [3, 7, 14, 30, 90];

    public async Task<IReadOnlyList<ExpenditureChangeModel>> Handle(GetExpenditureChangesQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;

        var latest = await _uow.Repository<ExpenditureSnapshot>().Query()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.SnapshotDate)
            .Select(s => (DateOnly?)s.SnapshotDate)
            .FirstOrDefaultAsync(ct);
        if (latest is not { } last)
            return Windows.Select(n => new ExpenditureChangeModel(n, null, ExpenditureChangeModel.Waiting)).ToList();

        var start = await _uow.Repository<MacroProgram>().Query()
            .Where(p => p.UserId == userId && p.IsActive)
            .Select(p => (DateOnly?)p.ExpenditureStartDate)
            .FirstOrDefaultAsync(ct);

        var from = last.AddDays(-Windows.Max());
        var series = await _uow.Repository<ExpenditureSnapshot>().Query()
            .Where(s => s.UserId == userId && s.SnapshotDate >= from && s.SnapshotDate <= last)
            .ToDictionaryAsync(s => s.SnapshotDate, s => s.EstimatedTdeeKcal, ct);

        return Windows.Select(n => ExpenditureCalculator.Change(series, last, n, start)).ToList();
    }
}
