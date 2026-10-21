namespace Application;

public sealed class UpsertProgressPhotoCommandHandler(
    IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider, IPrivateImageStore _store,
    IOptions<ProgressPhotoOptions> _options, IOptions<JwtOptions> _jwt, ILogger<UpsertProgressPhotoCommandHandler> _logger)
    : IRequestHandler<UpsertProgressPhotoCommand, ProgressPhotoModel>
{
    public async Task<ProgressPhotoModel> Handle(UpsertProgressPhotoCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var locale = await UserLocale.GetAsync(_uow, userId, _timeProvider, ct);
        var date = request.Date ?? locale.Today;
        locale.EnsureNotFuture(date, "Date");
        var now = _timeProvider.GetUtcNow();

        // Không tin Content-Type của client: loại ảnh lấy từ magic bytes
        var mediaType = ImageSniffer.Detect(request.Data) ?? throw new BadRequestException(ExceptionMessages.ImageTypeNotSupported);

        var consentAt = await ProgressPhotoSupport.FirstConsentAsync(_uow, userId, ct);
        if (consentAt is null)
        {
            if (!request.Consent) throw new BadRequestException(ExceptionMessages.PhotoConsentRequired);
            consentAt = now;
        }

        var clean = ImageMetadataStripper.Strip(request.Data, mediaType);
        var key = $"progress-photos/{userId:N}/{Guid.NewGuid():N}";
        await _store.UploadAsync(key, clean, ct);

        var row = await _uow.Repository<ProgressPhoto>().FindTrackedAsync(p => p.UserId == userId && p.PhotoDate == date && p.Angle == request.Angle, ct);
        var oldKey = row?.StorageKey;
        if (row is null)
        {
            row = new ProgressPhoto { Id = Guid.NewGuid(), UserId = userId, PhotoDate = date, Angle = request.Angle };
            await _uow.Repository<ProgressPhoto>().AddAsync(row, ct);
        }
        row.StorageKey = key;
        row.ContentType = mediaType;
        row.CapturedAt = now;
        row.ConsentGivenAt ??= consentAt;

        try { await _uow.EnsureSaveAsync(ct); }
        catch
        {
            await DeleteQuietlyAsync(key); // không để lại ảnh mồ côi ở kho khi lưu DB lỗi
            throw;
        }

        if (oldKey is not null) await DeleteQuietlyAsync(oldKey);
        return ProgressPhotoSupport.ToModel(row, _jwt.Value.SecretKey, _options.Value, now);
    }

    private async Task DeleteQuietlyAsync(string key)
    {
        try { await _store.DeleteAsync(key, CancellationToken.None); }
        catch (ExternalServiceException ex) { _logger.LogWarning(ex, "Không xoá được ảnh tiến độ cũ khỏi kho"); }
    }
}
