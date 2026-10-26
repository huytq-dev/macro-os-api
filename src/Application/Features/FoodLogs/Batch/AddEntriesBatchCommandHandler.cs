namespace Application;

public sealed class AddEntriesBatchCommandHandler(IUserClock _clock, IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider, IOptions<AlgorithmOptions> _options)
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
            // Replay chỉ hợp lệ khi lô gửi lại GIỐNG lô đã lưu (cùng món/recipe, kể cả món đã xoá mềm sau đó); key dùng cho lô KHÁC thì không được trả 200 như thể đã lưu (món mới bị bỏ im lặng)
            var stored = await _uow.Repository<FoodLogEntry>().Query().IgnoreQueryFilters()
                .Where(e => e.FoodLogBatchId == batchId)
                .OrderBy(e => e.CreatedAt).ThenBy(e => e.Id)
                .Select(e => new { e.Id, Item = e.FoodItemId != null ? e.FoodItemId : e.RecipeId })
                .ToListAsync(ct);
            var sameLot = stored.Select(s => s.Item).Order().SequenceEqual(request.Items.Select(i => i.FoodItemId ?? i.RecipeId).Order());
            if (!sameLot)
                throw new ConflictException(ExceptionMessages.IdempotencyKeyReused, new Dictionary<string, string[]> { ["reasonCode"] = ["IDEMPOTENCY_KEY_REUSED"] });
            return new BatchEntriesResponse(batchId, stored.Select(s => s.Id).ToList(), Replayed: true);
        }

        var lines = request.Items.Select(i => new EntryLine(i.FoodItemId, i.RecipeId, i.FoodServingId, i.Quantity)).ToList();
        var lookup = await FoodLogEntryFactory.LoadAsync(_uow, userId, lines, ct);
        var log = await FoodLogSupport.GetOrCreateAsync(_uow, userId, request.LogDate ?? (await _clock.GetAsync(ct)).Today, ct);

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
