namespace Application;

public sealed class GetNutritionDataQueryHandler(IUnitOfWork _uow, IUserContext _userContext) : IRequestHandler<GetNutritionDataQuery, NutritionDataResponse>
{
    // 90 ngày là KÍCH THƯỚC TRANG, không phải giới hạn lịch sử: ngày cũ hơn lấy tiếp bằng con trỏ `before` = nextBefore (không tải toàn bộ lịch sử)
    public const int DefaultLimit = 90, MaxLimit = 366;

    public async Task<NutritionDataResponse> Handle(GetNutritionDataQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var limit = Math.Clamp(request.Limit ?? DefaultLimit, 1, MaxLimit);
        var query = _uow.Repository<FoodLog>().Query().Where(l => l.UserId == userId
            && (l.Entries.Any() || l.IsFasting || l.ManualCaloriesKcal != null || l.ManualProteinG != null || l.ManualCarbsG != null || l.ManualFatG != null));
        if (request.From is { } from) query = query.Where(l => l.LogDate >= from);
        if (request.To is { } to) query = query.Where(l => l.LogDate <= to);
        if (request.Before is { } before) query = query.Where(l => l.LogDate < before);

        var rows = await query.OrderByDescending(l => l.LogDate).Take(limit + 1)
            .Select(l => new
            {
                l.LogDate, l.TotalCalories, l.TotalProtein, l.TotalCarbs, l.TotalFat, l.TotalFiber, l.TotalWater, l.IsFasting, l.IsIncomplete,
                Count = l.Entries.Count(),
                l.ManualCaloriesKcal, l.ManualProteinG, l.ManualCarbsG, l.ManualFatG,
            })
            .ToListAsync(ct);

        var hasMore = rows.Count > limit;
        var page = rows.Take(limit).ToList();
        var items = page.Select(r => NutritionDataSupport.ToModel(
            FoodLogSupport.LoggedDay.From(r.LogDate, r.TotalCalories, r.TotalProtein, r.TotalCarbs, r.TotalFat, r.TotalFiber, r.TotalWater, r.IsIncomplete, r.IsFasting, r.Count > 0,
                r.ManualCaloriesKcal, r.ManualProteinG, r.ManualCarbsG, r.ManualFatG), r.Count)).ToList();
        return new NutritionDataResponse(items, hasMore, hasMore ? page[^1].LogDate : null);
    }
}
