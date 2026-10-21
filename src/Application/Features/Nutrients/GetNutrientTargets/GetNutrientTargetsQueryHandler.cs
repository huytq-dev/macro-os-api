namespace Application;

public sealed class GetNutrientTargetsQueryHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<GetNutrientTargetsQuery, IReadOnlyList<NutrientModel>>
{
    public async Task<IReadOnlyList<NutrientModel>> Handle(GetNutrientTargetsQuery request, CancellationToken ct)
        => await NutrientCatalog.BuildAsync(_uow, _userContext.UserId, FoodLogSupport.Today(_timeProvider), withAmounts: false, ct, request.All);
}
