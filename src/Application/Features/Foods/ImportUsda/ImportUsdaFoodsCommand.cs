namespace Application;

/// <summary>
/// WP1: nhập món Foundation + SR Legacy từ USDA FoodData Central. Query (tìm theo từ khoá, lấy MaxItems món đầu) hoặc FdcIds (chỉ định).
/// Idempotent theo (ProviderCode = usda, ExternalFoodId = fdcId): món đã có thì bỏ qua. Mỗi lần chạy ghi một FoodImportRun. Mọi user đã đăng nhập đều chạy được.
/// </summary>
public sealed record ImportUsdaFoodsCommand(string? Query, List<int>? FdcIds, int MaxItems = 10) : IRequest<FoodImportRunModel>;

public sealed class ImportUsdaFoodsCommandValidator : AbstractValidator<ImportUsdaFoodsCommand>
{
    public ImportUsdaFoodsCommandValidator()
    {
        RuleFor(x => x).Must(x => !string.IsNullOrWhiteSpace(x.Query) || x.FdcIds is { Count: > 0 })
            .WithName("Query").WithMessage("Cần Query hoặc FdcIds.");
        RuleFor(x => x.Query).MaximumLength(200).WithMessage(ValidationMessages.MaxLength);
        RuleFor(x => x.MaxItems).InclusiveBetween(1, 50);
        RuleFor(x => x.FdcIds).Must(l => l is null || (l.Count <= 50 && l.All(i => i > 0))).WithMessage("Tối đa 50 fdcId dương.");
    }
}

public sealed class ImportUsdaFoodsCommandHandler(IUnitOfWork _uow, IUsdaClient _usda, TimeProvider _timeProvider)
    : IRequestHandler<ImportUsdaFoodsCommand, FoodImportRunModel>
{
    public async Task<FoodImportRunModel> Handle(ImportUsdaFoodsCommand request, CancellationToken ct)
    {
        var now = _timeProvider.GetUtcNow();
        var run = new FoodImportRun
        {
            Id = Guid.NewGuid(),
            Provider = "usda",
            Query = !string.IsNullOrWhiteSpace(request.Query) ? request.Query.Trim() : $"fdcIds: {string.Join(',', request.FdcIds!)}",
            Status = ImportRunStatus.Running,
            StartedAt = now,
        };
        await _uow.Repository<FoodImportRun>().AddAsync(run, ct);
        await _uow.EnsureSaveAsync(ct);

        try
        {
            var ids = (request.FdcIds is { Count: > 0 } given ? given : await _usda.SearchAsync(request.Query!.Trim(), request.MaxItems, ct))
                .Distinct().ToList();

            // Món đã nhập (kể cả đã xoá mềm: unique index vẫn giữ chỗ)
            var wanted = ids.Select(i => i.ToString()).ToList();
            var existing = (await _uow.Repository<FoodItem>().Query().IgnoreQueryFilters()
                .Where(f => f.ProviderCode == "usda" && wanted.Contains(f.ExternalFoodId!))
                .Select(f => f.ExternalFoodId!).ToListAsync(ct)).ToHashSet();
            var missing = ids.Where(i => !existing.Contains(i.ToString())).ToList();

            var fetched = missing.Count == 0 ? [] : await _usda.GetFoodsAsync(missing, ct);
            var byNumber = (await _uow.Repository<NutrientDefinition>().Query().Where(d => d.UsdaNumber != null)
                .Select(d => new { d.UsdaNumber, d.Id, d.Code, d.Unit }).ToListAsync(ct))
                .ToDictionary(d => d.UsdaNumber!, d => new UsdaFoodMapper.Definition(d.Id, d.Code, d.Unit));

            var imported = 0;
            var unusable = 0;
            var addedFoods = new List<FoodItem>();
            foreach (var u in fetched)
            {
                if (UsdaFoodMapper.Map(u, byNumber, now) is not { } mapped) { unusable++; continue; }
                await _uow.Repository<FoodItem>().AddAsync(mapped.Food, ct);
                addedFoods.Add(mapped.Food);
                foreach (var n in mapped.Nutrients)
                    await _uow.Repository<FoodNutrient>().AddAsync(new FoodNutrient
                    {
                        Id = Guid.NewGuid(), FoodItemId = mapped.Food.Id, NutrientDefinitionId = n.DefinitionId,
                        AmountPer100g = n.Amount, AmountStatus = NutrientAmountStatus.Measured,
                    }, ct);
                imported++;
            }

            // Gom vào thực phẩm chuẩn (một lượt truy vấn cho cả lô) thay vì để dòng rời
            await CanonicalLinker.LinkAsync(_uow, addedFoods, ct);

            // Id USDA không trả về cũng tính là bỏ qua có lỗi
            var notReturned = missing.Count - fetched.Count;
            run.Imported = imported;
            run.Skipped = existing.Count + unusable + Math.Max(notReturned, 0);
            run.Status = unusable + Math.Max(notReturned, 0) > 0 ? ImportRunStatus.Partial : ImportRunStatus.Succeeded;
            if (run.Status == ImportRunStatus.Partial)
                run.Error = $"{unusable} món thiếu năng lượng, {Math.Max(notReturned, 0)} id USDA không trả về.";
        }
        catch (ExternalServiceException ex)
        {
            run.Status = ImportRunStatus.Failed;
            run.Error = ex.Message;
            run.FinishedAt = _timeProvider.GetUtcNow();
            await _uow.EnsureSaveAsync(ct);
            throw;
        }

        run.FinishedAt = _timeProvider.GetUtcNow();
        await _uow.EnsureSaveAsync(ct);
        return run.Adapt<FoodImportRunModel>();
    }
}

/// <summary>WP1: các lần nhập gần nhất (mới nhất trước).</summary>
public sealed record GetFoodImportRunsQuery(int Top = 20) : IRequest<IReadOnlyList<FoodImportRunModel>>;

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

public sealed class FoodImportMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
        => config.NewConfig<FoodImportRun, FoodImportRunModel>().Map(d => d.Status, s => s.Status.ToString());
}
