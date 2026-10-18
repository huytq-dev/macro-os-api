namespace Application;

/// <summary>Một món/recipe trong plate. MealType/EatenAt của dòng thiếu → lấy của lô; lô cũng thiếu → suy như thêm đơn lẻ.</summary>
public sealed record BatchEntryItem(
    Guid? FoodItemId,
    Guid? RecipeId,
    Guid? FoodServingId,
    decimal Quantity,
    MealType? MealType = null,
    TimeOnly? EatenAt = null);

/// <summary>
/// WP3: log cả plate trong một giao dịch (thay vòng lặp tuần tự của FE). IdempotencyKey (≤ 100 ký tự, FE tạo mỗi lần bấm Log) chống nhân đôi khi retry:
/// key đã dùng → trả lại entry của lần đầu, không ghi gì. Tổng ngày và expenditure tính lại đúng một lần cho cả lô.
/// </summary>
public sealed record AddEntriesBatchCommand(
    string IdempotencyKey,
    DateOnly? LogDate,
    MealType? MealType,
    TimeOnly? EatenAt,
    string? Source,
    List<BatchEntryItem> Items
) : IRequest<BatchEntriesResponse>;

public sealed class AddEntriesBatchCommandValidator : AbstractValidator<AddEntriesBatchCommand>
{
    public const int MaxItems = 50;

    public AddEntriesBatchCommandValidator()
    {
        RuleFor(x => x.IdempotencyKey).NotEmpty().WithMessage(ValidationMessages.Required).MaximumLength(100).WithMessage(ValidationMessages.MaxLength);
        RuleFor(x => x.Source).MaximumLength(50).WithMessage(ValidationMessages.MaxLength);
        RuleFor(x => x.MealType).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);
        RuleFor(x => x.Items).NotEmpty().WithMessage(ValidationMessages.Required)
            .Must(i => i is null || i.Count <= MaxItems).WithMessage($"Tối đa {MaxItems} món mỗi lô.");
        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i).Must(i => i.FoodItemId.HasValue != i.RecipeId.HasValue)
                .WithName("FoodItemId").WithMessage("Mỗi dòng cần đúng một trong FoodItemId hoặc RecipeId.");
            item.RuleFor(i => i.FoodServingId).Null().When(i => i.RecipeId.HasValue).WithMessage("Recipe không có khẩu phần riêng.");
            item.RuleFor(i => i.Quantity).InclusiveBetween(0.01m, 10000);
            item.RuleFor(i => i.MealType).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);
        });
    }
}

public sealed class AddEntriesBatchCommandHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider, IOptions<AlgorithmOptions> _options)
    : IRequestHandler<AddEntriesBatchCommand, BatchEntriesResponse>
{
    public async Task<BatchEntriesResponse> Handle(AddEntriesBatchCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var key = request.IdempotencyKey.Trim();

        var previous = await _uow.Repository<FoodLogBatch>().Query()
            .Where(b => b.UserId == userId && b.IdempotencyKey == key)
            .Select(b => (Guid?)b.Id)
            .FirstOrDefaultAsync(ct);
        if (previous is { } batchId)
        {
            var ids = await _uow.Repository<FoodLogEntry>().Query()
                .Where(e => e.FoodLogBatchId == batchId)
                .OrderBy(e => e.CreatedAt).ThenBy(e => e.Id)
                .Select(e => e.Id)
                .ToListAsync(ct);
            return new BatchEntriesResponse(batchId, ids, Replayed: true);
        }

        var lines = request.Items.Select(i => new EntryLine(i.FoodItemId, i.RecipeId, i.FoodServingId, i.Quantity)).ToList();
        var lookup = await FoodLogEntryFactory.LoadAsync(_uow, userId, lines, ct);
        var log = await FoodLogSupport.GetOrCreateAsync(_uow, userId, request.LogDate ?? FoodLogSupport.Today(_timeProvider), ct);

        // Múi giờ user chỉ cần tra một lần, và chỉ khi có dòng thiếu giờ
        var needsNow = request.Items.Any(i => (i.EatenAt ?? request.EatenAt) is null);
        (DateOnly, TimeOnly)? now = needsNow ? await FoodLogSupport.LocalNowAsync(_uow, userId, _timeProvider, ct) : null;

        var batch = new FoodLogBatch { Id = Guid.NewGuid(), UserId = userId, IdempotencyKey = key, Source = request.Source?.Trim() };
        await _uow.Repository<FoodLogBatch>().AddAsync(batch, ct);

        var created = new List<Guid>(lines.Count);
        for (var i = 0; i < lines.Count; i++)
        {
            var item = request.Items[i];
            var (eatenAt, meal) = FoodLogSupport.ResolveTime(log.LogDate, item.EatenAt ?? request.EatenAt, item.MealType ?? request.MealType, now);
            created.Add((await FoodLogEntryFactory.AddAsync(_uow, lookup, lines[i], log, eatenAt, meal, batch.Id, ct)).Id);
        }
        FoodLogSupport.RecalcTotals(log);

        await FoodLogSupport.SaveAndRebuildAsync(_uow, userId, log.LogDate, _options.Value, _timeProvider, ct);
        return new BatchEntriesResponse(batch.Id, created, Replayed: false);
    }
}
