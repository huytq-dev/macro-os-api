namespace Application;

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
