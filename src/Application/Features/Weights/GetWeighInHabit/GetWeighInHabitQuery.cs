namespace Application;

/// <summary>Trang Habits (Weigh-In): ngày có cân/% mỡ của một tháng (lịch) hoặc cả năm (heatmap) + streak. Year mặc định năm hiện tại (theo múi giờ của user), Month null = cả năm.</summary>
public sealed record GetWeighInHabitQuery(int? Year, int? Month) : IRequest<WeighInHabitResponse>;

public sealed class GetWeighInHabitQueryValidator : AbstractValidator<GetWeighInHabitQuery>
{
    public GetWeighInHabitQueryValidator()
    {
        RuleFor(x => x.Year!.Value).InclusiveBetween(2000, DateTime.UtcNow.Year + 1).WithMessage(ValidationMessages.Range).When(x => x.Year.HasValue);
        RuleFor(x => x.Month!.Value).InclusiveBetween(1, 12).WithMessage(ValidationMessages.Range).When(x => x.Month.HasValue);
    }
}

public sealed class GetWeighInHabitQueryHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<GetWeighInHabitQuery, WeighInHabitResponse>
{
    public async Task<WeighInHabitResponse> Handle(GetWeighInHabitQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var today = (await FoodLogSupport.LocalNowAsync(_uow, userId, _timeProvider, ct)).Date;
        var year = request.Year ?? today.Year;
        var from = new DateOnly(year, request.Month ?? 1, 1);
        var to = request.Month is not null ? from.AddMonths(1).AddDays(-1) : new DateOnly(year, 12, 31);

        // Số liệu toàn thời gian (streak, tổng): chỉ cột ngày của các lần cân
        var allDates = await _uow.Repository<WeightEntry>().Query().Where(w => w.UserId == userId).Select(w => w.LogDate).ToListAsync(ct);
        var streaks = WeighInStreak.Compute(allDates, today);

        var weights = await _uow.Repository<WeightEntry>().Query()
            .Where(w => w.UserId == userId && w.LogDate >= from && w.LogDate <= to)
            .Select(w => new { w.Id, w.LogDate, w.WeightKg, w.TrendWeightKg }).ToListAsync(ct);
        // Nhiều phương pháp đo trong một ngày: lấy lần ghi sau cùng
        var bodyFat = (await _uow.Repository<BodyMetric>().Query()
                .Where(b => b.UserId == userId && b.BodyFatPercent != null && b.MeasuredDate >= from && b.MeasuredDate <= to)
                .OrderByDescending(b => b.CreatedAt)
                .Select(b => new { b.Id, b.MeasuredDate, b.Method, b.BodyFatPercent }).ToListAsync(ct))
            .GroupBy(b => b.MeasuredDate).ToDictionary(g => g.Key, g => g.First());

        var weightByDate = weights.ToDictionary(w => w.LogDate);
        var days = weightByDate.Keys.Concat(bodyFat.Keys).Distinct().Order().Select(d =>
        {
            weightByDate.TryGetValue(d, out var w);
            bodyFat.TryGetValue(d, out var b);
            return new WeighInDayModel(d, w?.Id, w?.WeightKg, w?.TrendWeightKg, b?.Id, b?.BodyFatPercent, b?.Method.ToString());
        }).ToList();

        return new WeighInHabitResponse(year, request.Month, days, weights.Count, allDates.Distinct().Count(), streaks.Current, streaks.Longest, streaks.IncludesToday,
            allDates.Count == 0 ? null : allDates.Min(), allDates.Count == 0 ? null : allDates.Max(), today);
    }
}
