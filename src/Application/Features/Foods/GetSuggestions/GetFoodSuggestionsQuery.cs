namespace Application;

/// <summary>
/// WP6 / UC-D06 Smart history: món user hay log quanh giờ <c>Time</c> (±90 phút, mặc định giờ hiện tại) và đúng thứ <c>Weekday</c> (bỏ qua = mọi thứ)
/// trong 90 ngày gần nhất, nhiều lần nhất trước. Không gồm quick add.
/// </summary>
public sealed record GetFoodSuggestionsQuery(TimeOnly? Time, DayOfWeek? Weekday, int Top = 8) : IRequest<IReadOnlyList<FoodSuggestionModel>>;

public sealed class GetFoodSuggestionsQueryValidator : AbstractValidator<GetFoodSuggestionsQuery>
{
    public GetFoodSuggestionsQueryValidator()
    {
        RuleFor(x => x.Top).InclusiveBetween(1, 30);
        RuleFor(x => x.Weekday).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue).When(x => x.Weekday.HasValue);
    }
}

public sealed class GetFoodSuggestionsQueryHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<GetFoodSuggestionsQuery, IReadOnlyList<FoodSuggestionModel>>
{
    private const int WindowMinutes = 90;
    private const int HistoryDays = 90;

    public async Task<IReadOnlyList<FoodSuggestionModel>> Handle(GetFoodSuggestionsQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var now = await FoodLogSupport.LocalNowAsync(_uow, userId, _timeProvider, ct);
        var time = request.Time ?? now.Time;
        var since = now.Date.AddDays(-HistoryDays);
        // Cửa sổ giờ không vòng qua nửa đêm: kẹp trong [00:00, 23:59]
        var from = time.AddMinutes(-WindowMinutes) <= time ? time.AddMinutes(-WindowMinutes) : TimeOnly.MinValue;
        var to = time.AddMinutes(WindowMinutes) >= time ? time.AddMinutes(WindowMinutes) : TimeOnly.MaxValue;

        var q = _uow.Repository<FoodLogEntry>().Query()
            .Where(e => e.FoodLog.UserId == userId && e.FoodLog.LogDate >= since && e.FoodItemId != null
                        && e.EatenAt >= from && e.EatenAt <= to && e.FoodItem!.Source != FoodSource.QuickAdd);
        // EF không dịch DateOnly.DayOfWeek sang SQL: liệt kê các ngày đúng thứ trong khoảng (~13 ngày) rồi lọc bằng IN
        if (request.Weekday is { } wd)
        {
            var days = Enumerable.Range(0, HistoryDays + 1).Select(i => since.AddDays(i)).Where(d => d.DayOfWeek == wd).ToList();
            q = q.Where(e => days.Contains(e.FoodLog.LogDate));
        }

        var top = await q.GroupBy(e => e.FoodItemId!.Value)
            .Select(g => new { FoodItemId = g.Key, Count = g.Count(), Last = g.Max(e => e.CreatedAt) })
            .OrderByDescending(x => x.Count).ThenByDescending(x => x.Last).ThenBy(x => x.FoodItemId)
            .Take(request.Top)
            .ToListAsync(ct);
        if (top.Count == 0) return [];

        var ids = top.Select(t => t.FoodItemId).ToList();
        var foods = await _uow.Repository<FoodItem>().Query()
            .Where(f => ids.Contains(f.Id))
            .Select(f => new
            {
                f.Id, f.Name, f.Brand, f.Category, f.Source, f.IsVerified, f.CreatedByUserId, f.IconKey,
                f.CaloriesPer100g, f.ProteinPer100g, f.CarbsPer100g, f.FatPer100g,
                Serving = f.Servings.OrderByDescending(s => s.IsDefault).Select(s => new { s.Id, s.ServingName, s.WeightInGrams }).FirstOrDefault(),
            })
            .ToDictionaryAsync(f => f.Id, ct);

        var overrides = await FoodIconSupport.OverridesAsync(_uow, userId, ids, ct);

        // Lần log gần nhất của từng món (khẩu phần + số lượng) để log lại một chạm
        var last = (await q.Where(e => ids.Contains(e.FoodItemId!.Value))
                .Select(e => new { FoodItemId = e.FoodItemId!.Value, e.FoodServingId, e.Quantity, e.CreatedAt })
                .ToListAsync(ct))
            .GroupBy(e => e.FoodItemId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(e => e.CreatedAt).First());

        return top.Where(t => foods.ContainsKey(t.FoodItemId)).Select(t =>
        {
            var f = foods[t.FoodItemId];
            var l = last[t.FoodItemId];
            var model = new FoodItemModel(f.Id, f.Name, f.Brand, f.Category.ToString(), f.Source.ToString(), f.IsVerified, f.CreatedByUserId == userId,
                f.CaloriesPer100g, f.ProteinPer100g, f.CarbsPer100g, f.FatPer100g,
                f.Serving?.Id, f.Serving?.ServingName ?? "100 g", f.Serving?.WeightInGrams ?? 100m, t.Last, FoodEmoji.For(f.Category),
                FoodIconSupport.Effective(f.Id, f.IconKey, overrides));
            return new FoodSuggestionModel(model, t.Count, l.FoodServingId, l.Quantity);
        }).ToList();
    }
}
