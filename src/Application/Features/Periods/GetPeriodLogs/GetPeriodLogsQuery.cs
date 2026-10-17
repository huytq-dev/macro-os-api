namespace Application;

/// <summary>UC-E05: các kỳ kinh (mới nhất trước) trong khoảng StartDate; mặc định 365 ngày gần nhất.</summary>
public sealed record GetPeriodLogsQuery(DateOnly? From, DateOnly? To) : IRequest<PeriodLogsResponse>;

public sealed class GetPeriodLogsQueryHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<GetPeriodLogsQuery, PeriodLogsResponse>
{
    public async Task<PeriodLogsResponse> Handle(GetPeriodLogsQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var today = FoodLogSupport.Today(_timeProvider);
        var to = request.To ?? today;
        var from = request.From ?? to.AddDays(-365);

        var items = await _uow.Repository<PeriodLog>().Query()
            .Where(p => p.UserId == userId && p.StartDate >= from && p.StartDate <= to)
            .OrderByDescending(p => p.StartDate)
            .Select(p => new PeriodLogModel(p.Id, p.StartDate, p.EndDate, p.CycleLengthDays, p.PeriodLengthDays, p.Symptoms, p.Notes))
            .ToListAsync(ct);

        // Kỳ gần nhất tính trên toàn lịch sử, không phụ thuộc khoảng hỏi
        var last = await _uow.Repository<PeriodLog>().Query()
            .Where(p => p.UserId == userId && p.StartDate <= today)
            .OrderByDescending(p => p.StartDate)
            .Select(p => (DateOnly?)p.StartDate)
            .FirstOrDefaultAsync(ct);

        int? daysSince = last is { } d ? today.DayNumber - d.DayNumber : null;
        return new PeriodLogsResponse(items, last, daysSince);
    }
}
