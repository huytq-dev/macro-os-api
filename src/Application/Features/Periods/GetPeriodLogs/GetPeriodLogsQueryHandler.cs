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
            throw new BadRequestException(ValidationMessages.DateRangeInvalid(GetPeriodLogsQueryValidator.MaxRangeDays));

        // Một truy vấn: mọi kỳ giao với [from, to] (StartDate ≤ to và còn kéo dài tới from); kỳ có StartDate trong khoảng là tập con
        var overlapping = await _uow.Repository<PeriodLog>().Query()
            .Where(p => p.UserId == userId && p.StartDate <= to && (p.EndDate == null || p.EndDate >= from))
            .OrderByDescending(p => p.StartDate)
            .ToListAsync(ct);
        var items = overlapping.Where(p => p.StartDate >= from)
            .Select(p => new PeriodLogModel(p.Id, p.StartDate, p.EndDate, p.CycleLengthDays, p.PeriodLengthDays, p.Symptoms, p.Notes)).ToList();
        var days = PeriodDays.Expand(overlapping.Select(p => new PeriodRange(p.Id, p.StartDate, p.EndDate, p.Symptoms, p.Notes)), from, to, today);

        // Kỳ gần nhất và NGÀY ghi nhận gần nhất tính trên toàn lịch sử (mọi kỳ bắt đầu ≤ hôm nay), không phụ thuộc khoảng hỏi
        var history = await _uow.Repository<PeriodLog>().Query()
            .Where(p => p.UserId == userId && p.StartDate <= today)
            .OrderByDescending(p => p.StartDate)
            .Select(p => new { p.StartDate, p.EndDate })
            .ToListAsync(ct);
        var last = history.FirstOrDefault();

        int? daysSince = last is { } l ? today.DayNumber - l.StartDate.DayNumber : null;
        int? lastLength = last is { } l2 ? PeriodDays.LengthDays(new PeriodRange(Guid.Empty, l2.StartDate, l2.EndDate, null, null), today) : null;
        var lastDay = PeriodDays.LastTrackedDay(history.Select(h => new PeriodRange(Guid.Empty, h.StartDate, h.EndDate, null, null)), today);
        return new PeriodLogsResponse(items, last?.StartDate, daysSince, days, lastLength, lastDay, lastDay is { } d ? today.DayNumber - d.DayNumber : null);
    }
}
