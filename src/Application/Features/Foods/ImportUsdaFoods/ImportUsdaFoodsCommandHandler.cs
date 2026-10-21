namespace Application;

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
