namespace Application;

public sealed class ConfirmAiScanCommandHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<ConfirmAiScanCommand, AiScanConfirmResponse>
{
    public async Task<AiScanConfirmResponse> Handle(ConfirmAiScanCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var now = _timeProvider.GetUtcNow();

        var scan = await _uow.Repository<FoodScanAnalysis>().FindTrackedAsync(s => s.Id == request.ScanId && s.UserId == userId, ct)
            ?? throw new NotFoundException(ExceptionMessages.NotFound("FoodScanAnalysis", request.ScanId));
        if (scan.Status is not (ScanStatus.Completed or ScanStatus.Confirmed))
            throw new ConflictException(ExceptionMessages.AiScanNotConfirmable);
        if (scan.ExpiresAt is { } exp && exp < now)
            throw new ConflictException(ExceptionMessages.AiScanExpired);

        // Món phải là món hệ thống hoặc của chính user; canonical phải tồn tại
        var foodIds = request.Ingredients.Where(i => i.FoodItemId.HasValue).Select(i => i.FoodItemId!.Value).Distinct().ToList();
        var okFoods = (await _uow.Repository<FoodItem>().Query()
            .Where(f => foodIds.Contains(f.Id) && (f.CreatedByUserId == null || f.CreatedByUserId == userId)).Select(f => f.Id).ToListAsync(ct)).ToHashSet();
        var canonIds = request.Ingredients.Where(i => i.CanonicalFoodId.HasValue).Select(i => i.CanonicalFoodId!.Value).Distinct().ToList();
        var okCanon = (await _uow.Repository<CanonicalFood>().Query().Where(c => canonIds.Contains(c.Id)).Select(c => c.Id).ToListAsync(ct)).ToHashSet();
        foreach (var i in request.Ingredients)
        {
            if (i.FoodItemId is { } f && !okFoods.Contains(f)) throw new NotFoundException(ExceptionMessages.NotFound("FoodItem", f));
            if (i.CanonicalFoodId is { } c && !okCanon.Contains(c)) throw new NotFoundException(ExceptionMessages.NotFound("CanonicalFood", c));
        }

        var picks = request.Ingredients
            .Select(i => (Norm: FoodNameNormalizer.Normalize(i.Name), Locale: FoodNameNormalizer.DetectLocale(i.Name), Item: i))
            .Where(p => p.Norm.Length > 0)
            // Khoá đệm có state (khớp ResolveAiIngredientQuery): "egg|fried"; state none/không gửi giữ khoá cũ "egg"
            .Select(p => (Norm: AiIngredientResolution.CacheKey(p.Norm, FoodPreparationState.Normalize(p.Item.State)), p.Locale, p.Item))
            .GroupBy(p => (p.Locale, p.Norm)).Select(g => g.Last()).ToList();

        var norms = picks.Select(p => p.Norm).ToList();
        var existing = (await _uow.Repository<AiIngredientCache>().GetAllTrackedAsync(c => c.UserId == userId && norms.Contains(c.NormalizedName), ct))
            .ToDictionary(c => (c.Locale, c.NormalizedName));

        foreach (var (norm, locale, item) in picks)
        {
            if (!existing.TryGetValue((locale, norm), out var row))
            {
                row = new AiIngredientCache { Id = Guid.NewGuid(), UserId = userId, NormalizedName = norm, Locale = locale };
                await _uow.Repository<AiIngredientCache>().AddAsync(row, ct);
            }
            row.FoodItemId = item.FoodItemId;
            row.CanonicalFoodId = item.CanonicalFoodId;
            row.LastUsedAt = now;
        }

        scan.Status = ScanStatus.Confirmed;
        await _uow.EnsureSaveAsync(ct);
        return new AiScanConfirmResponse(scan.Id, scan.Status.ToString(), picks.Count);
    }
}
