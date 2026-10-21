namespace Application;

public sealed class GetFoodImportRunsQueryHandler(IUnitOfWork _uow)
    : IRequestHandler<GetFoodImportRunsQuery, IReadOnlyList<FoodImportRunModel>>
{
    public async Task<IReadOnlyList<FoodImportRunModel>> Handle(GetFoodImportRunsQuery request, CancellationToken ct)
        => await _uow.Repository<FoodImportRun>().Query()
            .OrderByDescending(r => r.StartedAt).ThenBy(r => r.Id)
            .Take(Math.Clamp(request.Top, 1, AppConstants.MaxPageSize))
            .ProjectToType<FoodImportRunModel>()
            .ToListAsync(ct);
}
