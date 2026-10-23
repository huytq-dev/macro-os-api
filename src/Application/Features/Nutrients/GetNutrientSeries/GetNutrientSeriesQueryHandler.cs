namespace Application;

public sealed class GetNutrientSeriesQueryHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<GetNutrientSeriesQuery, NutrientSeriesResponse>
{
    public async Task<NutrientSeriesResponse> Handle(GetNutrientSeriesQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var nutrient = await NutrientDetailSupport.ResolveAsync(_uow, request.Code, ct);
        var locale = await UserLocale.GetAsync(_uow, userId, _timeProvider, ct);
        var to = request.To is { } t && t < locale.Today ? t : locale.Today;
        DateOnly? from = request.From ?? (request.All == true ? null : to.AddDays(-90));

        var (goal, goalKind, firstTargetDate, provenance) = await NutrientDetailSupport.GoalFnAsync(_uow, userId, nutrient.Code, to, ct, locale.Today);
        var (first, days) = await NutrientDetailSupport.LoadAsync(_uow, userId, nutrient.Code, from, to, goal, _timeProvider, ct, firstTargetDate, provenance);
        var decimals = NutrientDetailCalculator.Decimals(nutrient.Code);
        var buckets = request.Granularity is { } g ? NutrientDetailCalculator.Buckets(days, g, locale.WeekStart, decimals) : [];
        return new NutrientSeriesResponse(nutrient.Code, nutrient.Name, nutrient.Unit, nutrient.Precision, goalKind, first, to, days, NutrientDetailCalculator.Summary(days, first, to, decimals), buckets);
    }
}
