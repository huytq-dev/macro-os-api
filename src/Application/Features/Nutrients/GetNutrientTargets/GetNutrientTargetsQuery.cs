namespace Application;

/// <summary>UC-D09: danh mục nutrient kèm target hiệu lực + pin của user, không có lượng đã ăn (màn đặt target/pin).</summary>
public sealed record GetNutrientTargetsQuery : IRequest<IReadOnlyList<NutrientModel>>;

public sealed class GetNutrientTargetsQueryHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<GetNutrientTargetsQuery, IReadOnlyList<NutrientModel>>
{
    public async Task<IReadOnlyList<NutrientModel>> Handle(GetNutrientTargetsQuery request, CancellationToken ct)
        => await NutrientCatalog.BuildAsync(_uow, _userContext.UserId, FoodLogSupport.Today(_timeProvider), withAmounts: false, ct);
}
