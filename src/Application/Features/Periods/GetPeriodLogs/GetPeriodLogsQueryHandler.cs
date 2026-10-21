namespace Application;

public sealed class GetPeriodLogsQueryHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<GetPeriodLogsQuery, PeriodLogsResponse>
{
    public async Task<PeriodLogsResponse> Handle(GetPeriodLogsQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var today = (await UserLocale.GetAsync(_uow, userId, _timeProvider, ct)).Today;
        var to = request.To ?? today;
        var from = request.From ?? to.AddDays(-365);
        if (to.DayNumber - from.DayNumber + 1 > GetPeriodLogsQueryValidator.MaxRangeDays)
            throw new BadRequestException($"Range must be valid (From ≤ To) and at most {GetPeriodLogsQueryValidator.MaxRangeDays} days.");

        // Một truy vấn: mọi kỳ giao với [from, to] (StartDate ≤ to và còn kéo dài tới from); kỳ có StartDate trong khoảng là tập con
        var overlapping = await _uow.Repository<PeriodLog>().Query()
            .Where(p => p.UserId == userId && p.StartDate <= to && (p.EndDate == null || p.EndDate >= from))
            .OrderByDescending(p => p.StartDate)
            .ToListAsync(ct);
        var items = overlapping.Where(p => p.StartDate >= from)
            .Select(p => new PeriodLogModel(p.Id, p.StartDate, p.EndDate, p.CycleLengthDays, p.PeriodLengthDays, p.Symptoms, p.Notes)).ToList();
        var days = PeriodDays.Expand(overlapping.Select(p => new PeriodRange(p.Id, p.StartDate, p.EndDate, p.Symptoms, p.Notes)), from, to, today);

        // Kỳ gần nhất tính trên toàn lịch sử, không phụ thuộc khoảng hỏi
        var last = await _uow.Repository<PeriodLog>().Query()
            .Where(p => p.UserId == userId && p.StartDate <= today)
            .OrderByDescending(p => p.StartDate)
            .Select(p => new { p.StartDate, p.EndDate })
            .FirstOrDefaultAsync(ct);

        int? daysSince = last is { } l ? today.DayNumber - l.StartDate.DayNumber : null;
        int? lastLength = last is { } l2 ? PeriodDays.LengthDays(new PeriodRange(Guid.Empty, l2.StartDate, l2.EndDate, null, null), today) : null;
        return new PeriodLogsResponse(items, last?.StartDate, daysSince, days, lastLength);
    }
}
