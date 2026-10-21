namespace Application;

public sealed class GetNutritionDataQueryHandler(IUnitOfWork _uow, IUserContext _userContext) : IRequestHandler<GetNutritionDataQuery, NutritionDataResponse>
{
    public async Task<NutritionDataResponse> Handle(GetNutritionDataQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var query = _uow.Repository<FoodLog>().Query().Where(l => l.UserId == userId && (l.Entries.Any() || l.IsFasting));
        if (request.From is { } from) query = query.Where(l => l.LogDate >= from);
        if (request.To is { } to) query = query.Where(l => l.LogDate <= to);

        var rows = await query.OrderByDescending(l => l.LogDate)
            .Select(l => new
            {
                l.LogDate, l.TotalCalories, l.TotalProtein, l.TotalCarbs, l.TotalFat, l.IsFasting, l.IsIncomplete,
                Count = l.Entries.Count(),
                Manual = l.Entries.Count(e => e.Source == EntrySource.QuickAdd),
            })
            .ToListAsync(ct);

        var items = rows.Select(r =>
        {
            var fasting = r.IsFasting && r.Count == 0;
            return new NutritionDataDayModel(r.LogDate, fasting ? 0 : r.TotalCalories, fasting ? 0 : r.TotalProtein, fasting ? 0 : r.TotalFat, fasting ? 0 : r.TotalCarbs,
                NutritionDataSupport.SourceOf(fasting, r.Count, r.Manual), fasting, r.IsIncomplete, r.Count);
        }).ToList();
        return new NutritionDataResponse(items);
    }
}
