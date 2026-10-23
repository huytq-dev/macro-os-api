namespace Application;

public sealed class GetMacroSeriesQueryHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<GetMacroSeriesQuery, MacroSeriesResponse>
{
    public async Task<MacroSeriesResponse> Handle(GetMacroSeriesQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var locale = await UserLocale.GetAsync(_uow, userId, _timeProvider, ct);
        var to = request.To is { } t && t < locale.Today ? t : locale.Today;
        DateOnly? from = request.From ?? (request.All == true ? null : to.AddDays(-90));
        if (from is { } f && to.DayNumber - f.DayNumber + 1 > NutrientDetailSupport.MaxRangeDays) throw new BadRequestException(ValidationMessages.RangeTooLong(NutrientDetailSupport.MaxRangeDays));

        var logs = await FoodLogSupport.LoadDaysAsync(_uow, userId, from ?? DateOnly.MinValue, to, ct, includeFasting: true);
        var first = from ?? NutrientDetailSupport.FirstDay(logs.Count > 0 ? logs.Keys.Min() : null, null, to);
        var days = MacroSeriesCalculator.Days(first, to, logs);
        var buckets = request.Granularity is { } g ? MacroSeriesCalculator.Buckets(days, g, locale.WeekStart) : [];
        return new MacroSeriesResponse(first, to, days, MacroSeriesCalculator.Summary(days, first, to), buckets);
    }
}
