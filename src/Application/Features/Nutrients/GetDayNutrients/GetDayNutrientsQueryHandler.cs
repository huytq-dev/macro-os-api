namespace Application;

public sealed class GetDayNutrientsQueryHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<GetDayNutrientsQuery, DayNutrientsResponse>
{
    public async Task<DayNutrientsResponse> Handle(GetDayNutrientsQuery request, CancellationToken ct)
    {
        var date = request.Date ?? FoodLogSupport.Today(_timeProvider);
        var items = await NutrientCatalog.BuildAsync(_uow, _userContext.UserId, date, withAmounts: true, ct, request.All);
        return new DayNutrientsResponse(date, items);
    }
}
