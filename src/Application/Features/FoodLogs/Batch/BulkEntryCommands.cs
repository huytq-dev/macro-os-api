namespace Application;

/// <summary>Tải các entry của user theo id (tracking) cùng toàn bộ entries của log chứa chúng để tính lại tổng; id không phải của user bị bỏ qua.</summary>
internal static class BulkEntrySupport
{
    public const int MaxIds = 200;

    public static async Task<(List<FoodLogEntry> Selected, List<FoodLog> Logs)> LoadOwnedAsync(IUnitOfWork uow, Guid userId, IReadOnlyCollection<Guid> ids, CancellationToken ct)
    {
        var logIds = await uow.Repository<FoodLogEntry>().Query()
            .Where(e => ids.Contains(e.Id) && e.FoodLog.UserId == userId)
            .Select(e => e.FoodLogId).Distinct()
            .ToListAsync(ct);
        if (logIds.Count == 0) return ([], []);

        var logs = await uow.Repository<FoodLog>().Query(tracking: true)
            .Include(l => l.Entries)
            .Where(l => logIds.Contains(l.Id))
            .ToListAsync(ct);
        var selected = logs.SelectMany(l => l.Entries).Where(e => !e.IsDeleted && ids.Contains(e.Id)).ToList();
        return (selected, logs);
    }

    public static void Ids<T>(AbstractValidator<T> v, Expression<Func<T, List<Guid>>> ids)
        => v.RuleFor(ids).NotEmpty().WithMessage(ValidationMessages.Required)
            .Must(l => l is null || l.Count <= MaxIds).WithMessage($"Tối đa {MaxIds} món mỗi lần.")
            .Must(l => l is null || l.All(i => i != Guid.Empty)).WithMessage("Id không hợp lệ.");
}

/// <summary>WP3 / Select All: xoá (soft) nhiều entry của mình; trả số entry đã xoá. Tổng ngày và expenditure tính lại một lần.</summary>
public sealed record BulkDeleteEntriesCommand(List<Guid> Ids) : IRequest<int>;

public sealed class BulkDeleteEntriesCommandValidator : AbstractValidator<BulkDeleteEntriesCommand>
{
    public BulkDeleteEntriesCommandValidator() => BulkEntrySupport.Ids(this, x => x.Ids);
}

public sealed class BulkDeleteEntriesCommandHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider, IOptions<AlgorithmOptions> _options)
    : IRequestHandler<BulkDeleteEntriesCommand, int>
{
    public async Task<int> Handle(BulkDeleteEntriesCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var (selected, logs) = await BulkEntrySupport.LoadOwnedAsync(_uow, userId, request.Ids, ct);
        if (selected.Count == 0) return 0;

        foreach (var e in selected)
        {
            await _uow.Repository<FoodLogEntry>().RemoveAsync(e, ct); // soft delete qua interceptor
            e.IsDeleted = true; // để RecalcTotals bỏ qua ngay trong cùng unit of work
        }
        var touched = logs.Where(l => l.Entries.Any(e => selected.Contains(e))).ToList();
        foreach (var l in touched) FoodLogSupport.RecalcTotals(l);

        var dates = touched.Select(l => l.LogDate).Distinct().ToList();
        await FoodLogSupport.SaveAndRebuildAsync(_uow, userId, dates.Min(), _options.Value, _timeProvider, ct, dates);
        return selected.Count;
    }
}

/// <summary>
/// WP3: copy nhiều entry sang ToDate (snapshot copy nguyên, không đọc lại món). ToTime thiếu → giữ giờ gốc; ToMealType thiếu → giữ bữa gốc
/// (có ToTime mà không có ToMealType → bữa suy từ ToTime). Trả số entry đã copy.
/// </summary>
public sealed record BulkCopyEntriesCommand(List<Guid> Ids, DateOnly ToDate, TimeOnly? ToTime, MealType? ToMealType) : IRequest<int>;

public sealed class BulkCopyEntriesCommandValidator : AbstractValidator<BulkCopyEntriesCommand>
{
    public BulkCopyEntriesCommandValidator()
    {
        BulkEntrySupport.Ids(this, x => x.Ids);
        RuleFor(x => x.ToMealType).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue).When(x => x.ToMealType.HasValue);
    }
}

public sealed class BulkCopyEntriesCommandHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider, IOptions<AlgorithmOptions> _options)
    : IRequestHandler<BulkCopyEntriesCommand, int>
{
    public async Task<int> Handle(BulkCopyEntriesCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var source = await _uow.Repository<FoodLogEntry>().Query()
            .Where(e => request.Ids.Contains(e.Id) && e.FoodLog.UserId == userId)
            .OrderBy(e => e.EatenAt).ThenBy(e => e.CreatedAt)
            .ToListAsync(ct);
        if (source.Count == 0) return 0;

        var target = await FoodLogSupport.GetOrCreateAsync(_uow, userId, request.ToDate, ct);
        var copied = new Dictionary<Guid, Guid>(); // entry nguồn → entry đích, để copy snapshot nutrient
        foreach (var s in source)
        {
            var copy = new FoodLogEntry
            {
                Id = Guid.NewGuid(),
                FoodLogId = target.Id,
                EatenAt = request.ToTime ?? s.EatenAt,
                MealType = request.ToMealType ?? (request.ToTime is { } t ? MealTimes.ForTime(t) : s.MealType),
                FoodItemId = s.FoodItemId,
                RecipeId = s.RecipeId,
                RecipeVersionId = s.RecipeVersionId,
                Source = s.Source,
                FoodServingId = s.FoodServingId,
                Quantity = s.Quantity,
                Calories = s.Calories, Protein = s.Protein, Carbs = s.Carbs, Fat = s.Fat,
                Fiber = s.Fiber, Sodium = s.Sodium,
            };
            target.Entries.Add(copy);
            copied[s.Id] = copy.Id;
        }
        await NutrientSnapshot.CopyAsync(_uow, copied, ct);
        FoodLogSupport.RecalcTotals(target);

        await FoodLogSupport.SaveAndRebuildAsync(_uow, userId, request.ToDate, _options.Value, _timeProvider, ct);
        return source.Count;
    }
}

/// <summary>
/// WP3: chuyển nhiều entry sang ngày/giờ/bữa khác (giữ nguyên Id và snapshot). Cần ít nhất một trong ToDate, ToTime, ToMealType.
/// ToTime mà không có ToMealType → bữa suy từ ToTime. Trả số entry đã chuyển; tổng của mọi ngày bị ảnh hưởng và expenditure tính lại một lần.
/// </summary>
public sealed record BulkMoveEntriesCommand(List<Guid> Ids, DateOnly? ToDate, TimeOnly? ToTime, MealType? ToMealType) : IRequest<int>;

public sealed class BulkMoveEntriesCommandValidator : AbstractValidator<BulkMoveEntriesCommand>
{
    public BulkMoveEntriesCommandValidator()
    {
        BulkEntrySupport.Ids(this, x => x.Ids);
        RuleFor(x => x).Must(x => x.ToDate.HasValue || x.ToTime.HasValue || x.ToMealType.HasValue)
            .WithName("ToDate").WithMessage("Cần ít nhất một trong ToDate, ToTime, ToMealType.");
        RuleFor(x => x.ToMealType).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue).When(x => x.ToMealType.HasValue);
    }
}

public sealed class BulkMoveEntriesCommandHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider, IOptions<AlgorithmOptions> _options)
    : IRequestHandler<BulkMoveEntriesCommand, int>
{
    public async Task<int> Handle(BulkMoveEntriesCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var (selected, logs) = await BulkEntrySupport.LoadOwnedAsync(_uow, userId, request.Ids, ct);
        if (selected.Count == 0) return 0;

        var touched = logs.Where(l => l.Entries.Any(e => selected.Contains(e))).ToDictionary(l => l.LogDate);
        FoodLog? target = null;
        if (request.ToDate is { } toDate)
        {
            target = touched.TryGetValue(toDate, out var same) ? same : await FoodLogSupport.GetOrCreateAsync(_uow, userId, toDate, ct);
            touched[toDate] = target;
        }

        foreach (var e in selected)
        {
            if (request.ToTime is { } t)
            {
                e.EatenAt = t;
                e.MealType = request.ToMealType ?? MealTimes.ForTime(t);
            }
            else if (request.ToMealType is { } m)
            {
                e.MealType = m;
            }

            if (target is not null && e.FoodLogId != target.Id)
            {
                e.FoodLog.Entries.Remove(e);
                target.Entries.Add(e); // EF đổi FoodLogId theo navigation
            }
        }
        foreach (var l in touched.Values) FoodLogSupport.RecalcTotals(l);

        var dates = touched.Keys.ToList();
        await FoodLogSupport.SaveAndRebuildAsync(_uow, userId, dates.Min(), _options.Value, _timeProvider, ct, dates);
        return selected.Count;
    }
}
