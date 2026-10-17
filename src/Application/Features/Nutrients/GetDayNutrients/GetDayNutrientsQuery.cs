namespace Application;

/// <summary>UC-D09: mọi nutrient của một ngày (mặc định hôm nay) kèm target hiệu lực và pin — dữ liệu cho trang Nutrition và các tile Dashboard.</summary>
public sealed record GetDayNutrientsQuery(DateOnly? Date) : IRequest<DayNutrientsResponse>;

public sealed class GetDayNutrientsQueryHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<GetDayNutrientsQuery, DayNutrientsResponse>
{
    public async Task<DayNutrientsResponse> Handle(GetDayNutrientsQuery request, CancellationToken ct)
    {
        var date = request.Date ?? FoodLogSupport.Today(_timeProvider);
        var items = await NutrientCatalog.BuildAsync(_uow, _userContext.UserId, date, withAmounts: true, ct);
        return new DayNutrientsResponse(date, items);
    }
}
