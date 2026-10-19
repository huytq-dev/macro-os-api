namespace Application;

/// <summary>
/// Pha 1 của AI Plate: nhận diện món + nguyên liệu + gram từ ảnh (0–3) và/hoặc mô tả, KHÔNG có dinh dưỡng (pha 2 tìm từng nguyên liệu trong DB).
/// Lưu kết quả (không lưu ảnh) vào FoodScanAnalysis và trả ScanId. ISensitiveRequest để không log byte ảnh.
/// </summary>
public sealed record AnalyzeMealCommand(IReadOnlyList<MealPhoto> Photos, string? Description)
    : IRequest<AiAnalysisResponse>, ISensitiveRequest;

public sealed class AnalyzeMealCommandValidator : AbstractValidator<AnalyzeMealCommand>
{
    public AnalyzeMealCommandValidator(IOptions<AiOptions> options)
    {
        var o = options.Value;
        var maxMb = o.MaxPhotoBytes / (1024 * 1024);

        RuleFor(x => x.Photos)
            .Must((cmd, photos) => photos.Count > 0 || !string.IsNullOrWhiteSpace(cmd.Description))
            .WithMessage(ValidationMessages.AiMealNeedsInput)
            .Must(photos => photos.Count <= o.MaxPhotos)
            .WithMessage(ValidationMessages.AiMealMaxPhotos(o.MaxPhotos));
        RuleForEach(x => x.Photos)
            .Must(p => p.Data.Length > 0 && p.Data.Length <= o.MaxPhotoBytes)
            .WithMessage(ValidationMessages.AiMealPhotoMaxMb(maxMb))
            .Must(p => p.MediaType is not null)
            .WithMessage(ValidationMessages.AiMealPhotoType);
        RuleFor(x => x.Description).MaximumLength(o.MaxDescriptionLength).WithMessage(ValidationMessages.MaxLength);
    }
}

public sealed class AnalyzeMealCommandHandler(
    IAiMealAnalyzer _analyzer, IAiCanonicalNames _canonicalNames, IOptions<AiOptions> _options, IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<AnalyzeMealCommand, AiAnalysisResponse>
{
    public async Task<AiAnalysisResponse> Handle(AnalyzeMealCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_options.Value.ApiKey))
            throw new ServiceUnavailableException(ExceptionMessages.AiNotConfigured);

        var draft = await _analyzer.AnalyzeAsync(request.Photos, request.Description?.Trim(), await _canonicalNames.GetAsync(ct), ct);
        var result = AiPlateNormalizer.Normalize(draft);

        // Chỉ lưu kết quả + metadata; ảnh không bao giờ được lưu. Kết quả chưa phải nguồn sự thật tới khi user Confirm
        var scan = new FoodScanAnalysis
        {
            Id = Guid.NewGuid(),
            UserId = _userContext.UserId,
            Mode = request.Photos.Count > 0 ? ScanMode.Photo : ScanMode.Describe,
            Status = ScanStatus.Completed,
            Provider = "anthropic",
            ModelVersion = _options.Value.Model,
            ResultJson = System.Text.Json.JsonSerializer.Serialize(result),
            Confidence = result.Confidence switch { "high" => 0.9m, "medium" => 0.7m, _ => 0.4m },
            ExpiresAt = _timeProvider.GetUtcNow().AddDays(30),
        };
        await _uow.Repository<FoodScanAnalysis>().AddAsync(scan, ct);
        await _uow.EnsureSaveAsync(ct);
        return result with { ScanId = scan.Id };
    }
}
