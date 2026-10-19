namespace Application;

public static class ProgressPhotoSupport
{
    public static ProgressPhotoModel ToModel(ProgressPhoto row, string secret, ProgressPhotoOptions opt, DateTimeOffset now)
    {
        var expires = now.AddMinutes(opt.SignedUrlMinutes);
        var exp = expires.ToUnixTimeSeconds();
        return new ProgressPhotoModel(row.Id, row.Angle.ToString(), ProgressPhotoUrlSigner.Url(row.Id, exp, ProgressPhotoUrlSigner.Sign(secret, row.Id, row.UserId, exp)), expires, row.CapturedAt);
    }

    /// <summary>Thời điểm user đồng ý lần đầu (kể cả khi ảnh đã xoá: đồng ý không mất khi xoá ảnh); null nếu chưa từng.</summary>
    public static async Task<DateTimeOffset?> FirstConsentAsync(IUnitOfWork uow, Guid userId, CancellationToken ct)
        => await uow.Repository<ProgressPhoto>().Query().IgnoreQueryFilters()
            .Where(p => p.UserId == userId && p.ConsentGivenAt != null)
            .OrderBy(p => p.ConsentGivenAt)
            .Select(p => p.ConsentGivenAt)
            .FirstOrDefaultAsync(ct);
}

/// <summary>Ảnh tiến độ của một ngày (mặc định hôm nay) kèm URL xem do BE ký (hết hạn ngắn) và cờ đã đồng ý riêng tư.</summary>
public sealed record GetProgressPhotosQuery(DateOnly? Date) : IRequest<ProgressPhotosResponse>;

public sealed class GetProgressPhotosQueryHandler(
    IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider, IOptions<ProgressPhotoOptions> _options, IOptions<JwtOptions> _jwt)
    : IRequestHandler<GetProgressPhotosQuery, ProgressPhotosResponse>
{
    public async Task<ProgressPhotosResponse> Handle(GetProgressPhotosQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var date = request.Date ?? FoodLogSupport.Today(_timeProvider);
        var now = _timeProvider.GetUtcNow();

        var rows = await _uow.Repository<ProgressPhoto>().Query()
            .Where(p => p.UserId == userId && p.PhotoDate == date)
            .OrderBy(p => p.Angle)
            .ToListAsync(ct);
        var consent = await ProgressPhotoSupport.FirstConsentAsync(_uow, userId, ct);

        return new ProgressPhotosResponse(date, consent is not null,
            rows.Select(r => ProgressPhotoSupport.ToModel(r, _jwt.Value.SecretKey, _options.Value, now)).ToList());
    }
}

/// <summary>
/// Lưu (hoặc thay) ảnh của (ngày, góc). Kiểm magic bytes (JPEG/PNG/WebP), bỏ EXIF/GPS, đẩy lên kho RIÊNG TƯ, chỉ lưu StorageKey. Ảnh không bao giờ được ghi log.
/// Lần đầu của user bắt buộc <paramref name="Consent"/> = true (ghi ConsentGivenAt).
/// </summary>
public sealed record UpsertProgressPhotoCommand(DateOnly? Date, PhotoAngle Angle, byte[] Data, bool Consent) : IRequest<ProgressPhotoModel>, ISensitiveRequest;

public sealed class UpsertProgressPhotoCommandValidator : AbstractValidator<UpsertProgressPhotoCommand>
{
    public UpsertProgressPhotoCommandValidator(IOptions<ProgressPhotoOptions> options, TimeProvider timeProvider)
    {
        RuleFor(x => x.Angle).Must(a => a is PhotoAngle.Front or PhotoAngle.Side or PhotoAngle.Back)
            .WithMessage(ValidationMessages.InvalidPhotoAngle);
        RuleFor(x => x.Data).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.Data).Must(d => d.Length <= options.Value.MaxBytes)
            .WithMessage(ValidationMessages.PhotoMaxMb(options.Value.MaxBytes / (1024 * 1024)))
            .When(x => x.Data is { Length: > 0 });
        RuleFor(x => x.Date)
            .LessThanOrEqualTo(_ => DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime.AddDays(1)))
            .WithMessage(ValidationMessages.NotInFuture)
            .When(x => x.Date.HasValue);
    }
}

public sealed class UpsertProgressPhotoCommandHandler(
    IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider, IPrivateImageStore _store,
    IOptions<ProgressPhotoOptions> _options, IOptions<JwtOptions> _jwt, ILogger<UpsertProgressPhotoCommandHandler> _logger)
    : IRequestHandler<UpsertProgressPhotoCommand, ProgressPhotoModel>
{
    public async Task<ProgressPhotoModel> Handle(UpsertProgressPhotoCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var date = request.Date ?? FoodLogSupport.Today(_timeProvider);
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

/// <summary>Xoá mọi ảnh của một ngày (mặc định hôm nay): xoá mềm dòng DB và xoá file khỏi kho riêng tư.</summary>
public sealed record DeleteProgressPhotosCommand(DateOnly? Date) : IRequest<Unit>;

public sealed class DeleteProgressPhotosCommandHandler(
    IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider, IPrivateImageStore _store, ILogger<DeleteProgressPhotosCommandHandler> _logger)
    : IRequestHandler<DeleteProgressPhotosCommand, Unit>
{
    public async Task<Unit> Handle(DeleteProgressPhotosCommand request, CancellationToken ct)
    {
        var date = request.Date ?? FoodLogSupport.Today(_timeProvider);
        var rows = await _uow.Repository<ProgressPhoto>().GetAllTrackedAsync(p => p.UserId == _userContext.UserId && p.PhotoDate == date, ct);
        if (rows.Count == 0) return Unit.Value;

        var keys = rows.Select(r => r.StorageKey).ToList();
        await _uow.Repository<ProgressPhoto>().RemoveRangeAsync(rows, ct); // interceptor chuyển thành soft delete
        await _uow.EnsureSaveAsync(ct);

        foreach (var key in keys)
        {
            try { await _store.DeleteAsync(key, CancellationToken.None); }
            catch (ExternalServiceException ex) { _logger.LogWarning(ex, "Không xoá được ảnh tiến độ khỏi kho"); }
        }
        return Unit.Value;
    }
}

/// <summary>Nội dung ảnh cho URL BE đã ký: chữ ký đúng + chưa hết hạn mới trả; sai/hết hạn/không tồn tại đều 404 (không lộ ảnh có tồn tại hay không).</summary>
public sealed record GetProgressPhotoContentQuery(Guid Id, long Exp, string? Sig) : IRequest<ProgressPhotoContent>, ISensitiveRequest;

public sealed class GetProgressPhotoContentQueryHandler(IUnitOfWork _uow, TimeProvider _timeProvider, IPrivateImageStore _store, IOptions<JwtOptions> _jwt)
    : IRequestHandler<GetProgressPhotoContentQuery, ProgressPhotoContent>
{
    public async Task<ProgressPhotoContent> Handle(GetProgressPhotoContentQuery request, CancellationToken ct)
    {
        var row = await _uow.Repository<ProgressPhoto>().FindAsync(p => p.Id == request.Id, ct);
        if (row is null || !ProgressPhotoUrlSigner.Verify(_jwt.Value.SecretKey, row.Id, row.UserId, request.Exp, request.Sig, _timeProvider.GetUtcNow()))
            throw new NotFoundException(ExceptionMessages.NotFound("ProgressPhoto", request.Id));

        var data = await _store.DownloadAsync(row.StorageKey, ct)
            ?? throw new NotFoundException(ExceptionMessages.NotFound("ProgressPhoto", request.Id));
        return new ProgressPhotoContent(data, row.ContentType);
    }
}
