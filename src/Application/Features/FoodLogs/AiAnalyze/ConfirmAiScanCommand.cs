namespace Application;

/// <summary>Nguyên liệu user đã chọn/xác nhận cho một tên AI: cần FoodItemId hoặc CanonicalFoodId.</summary>
public sealed record AiConfirmedIngredient(string Name, Guid? FoodItemId, Guid? CanonicalFoodId);

/// <summary>
/// Khi user log món từ kết quả AI: chuyển FoodScanAnalysis Completed → Confirmed và nhớ lựa chọn từng nguyên liệu (AiIngredientCache, theo user)
/// để lần sau resolve-ingredient trả đúng món đó. Gọi lại với cùng scan thì cập nhật bộ nhớ (idempotent).
/// </summary>
public sealed record ConfirmAiScanCommand(Guid ScanId, List<AiConfirmedIngredient> Ingredients) : IRequest<AiScanConfirmResponse>;

public sealed class ConfirmAiScanCommandValidator : AbstractValidator<ConfirmAiScanCommand>
{
    public ConfirmAiScanCommandValidator()
    {
        RuleFor(x => x.ScanId).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.Ingredients).NotNull().WithMessage(ValidationMessages.Required)
            .Must(l => l is null || l.Count <= 50).WithMessage("Tối đa 50 nguyên liệu.");
        RuleForEach(x => x.Ingredients).ChildRules(i =>
        {
            i.RuleFor(x => x.Name).NotEmpty().WithMessage(ValidationMessages.Required).MaximumLength(100).WithMessage(ValidationMessages.MaxLength);
            i.RuleFor(x => x).Must(x => x.FoodItemId.HasValue || x.CanonicalFoodId.HasValue)
                .WithName("FoodItemId").WithMessage("Cần FoodItemId hoặc CanonicalFoodId.");
        });
    }
}

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
            throw new ConflictException("Phân tích này không còn dùng được để xác nhận.");
        if (scan.ExpiresAt is { } exp && exp < now)
            throw new ConflictException("Phân tích đã hết hạn, hãy phân tích lại.");

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
