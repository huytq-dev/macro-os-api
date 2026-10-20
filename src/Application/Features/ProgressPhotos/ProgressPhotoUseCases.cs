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
        var date = request.Date ?? (await UserLocale.GetAsync(_uow, userId, _timeProvider, ct)).Today;
        var now = _timeProvider.GetUtcNow();

        var rows = (await _uow.Repository<ProgressPhoto>().Query()
            .Where(p => p.UserId == userId && p.PhotoDate == date)
            .ToListAsync(ct)).OrderBy(r => (int)r.Angle).ToList(); // Front, Side, Back (cột Angle lưu chuỗi nên không sắp theo SQL được)
        var consent = await ProgressPhotoSupport.FirstConsentAsync(_uow, userId, ct);
        var weights = await FullBodyWriter.WeightsAsync(_uow, userId, date, date, ct);
        var hasWeight = weights.TryGetValue(date, out var weight);

        return new ProgressPhotosResponse(date, consent is not null,
            rows.Select(r => ProgressPhotoSupport.ToModel(r, _jwt.Value.SecretKey, _options.Value, now)).ToList(),
            hasWeight ? weight.Scale : null, hasWeight ? weight.Trend : null);
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

/// <summary>
/// Xoá ảnh của một ngày (mặc định hôm nay theo múi giờ user): không có Angle = cả phiên (mọi tư thế), có Angle = chỉ tư thế đó. Xoá mềm dòng DB và xoá file khỏi kho riêng tư; không có gì để xoá vẫn thành công.
/// </summary>
public sealed record DeleteProgressPhotosCommand(DateOnly? Date, PhotoAngle? Angle = null) : IRequest<Unit>;

public sealed class DeleteProgressPhotosCommandHandler(
    IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider, IPrivateImageStore _store, ILogger<DeleteProgressPhotosCommandHandler> _logger)
    : IRequestHandler<DeleteProgressPhotosCommand, Unit>
{
    public async Task<Unit> Handle(DeleteProgressPhotosCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var date = request.Date ?? (await UserLocale.GetAsync(_uow, userId, _timeProvider, ct)).Today;
        var angle = request.Angle;
        var rows = await _uow.Repository<ProgressPhoto>().GetAllTrackedAsync(p => p.UserId == userId && p.PhotoDate == date && (angle == null || p.Angle == angle), ct);
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

/// <summary>
/// Danh sách phiên ảnh theo ngày (mới nhất trước, chỉ ngày có ít nhất một ảnh), mỗi phiên kèm các tư thế (Front, Side, Back; URL xem có chữ ký như GET ?date=) và cân/trend của ngày đó.
/// From/To tuỳ chọn (gồm hai đầu), mặc định mọi ngày; Limit cắt theo SỐ PHIÊN (ngày mới nhất), tối đa 1000. Chỉ ảnh của chính user.
/// </summary>
public sealed record GetProgressPhotoSessionsQuery(DateOnly? From, DateOnly? To, int? Limit = null) : IRequest<ProgressPhotoSessionsResponse>;

public sealed class GetProgressPhotoSessionsQueryValidator : AbstractValidator<GetProgressPhotoSessionsQuery>
{
    public GetProgressPhotoSessionsQueryValidator()
        => RuleFor(x => x).Must(x => x.From is null || x.To is null || x.From <= x.To).WithName("From").WithMessage("From must not be after To.");
}

public sealed class GetProgressPhotoSessionsQueryHandler(
    IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider, IOptions<ProgressPhotoOptions> _options, IOptions<JwtOptions> _jwt)
    : IRequestHandler<GetProgressPhotoSessionsQuery, ProgressPhotoSessionsResponse>
{
    public async Task<ProgressPhotoSessionsResponse> Handle(GetProgressPhotoSessionsQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var now = _timeProvider.GetUtcNow();
        var consent = await ProgressPhotoSupport.FirstConsentAsync(_uow, userId, ct);

        var query = _uow.Repository<ProgressPhoto>().Query().Where(p => p.UserId == userId);
        if (request.From is { } from) query = query.Where(p => p.PhotoDate >= from);
        if (request.To is { } to) query = query.Where(p => p.PhotoDate <= to);

        if (request.Limit is { } limit)
        {
            var dates = await query.Select(p => p.PhotoDate).Distinct().OrderByDescending(d => d).Take(Math.Clamp(limit, 1, 1000)).ToListAsync(ct);
            if (dates.Count == 0) return new ProgressPhotoSessionsResponse(consent is not null, []);
            query = query.Where(p => dates.Contains(p.PhotoDate));
        }
        var rows = await query.ToListAsync(ct);
        if (rows.Count == 0) return new ProgressPhotoSessionsResponse(consent is not null, []);

        var weights = await FullBodyWriter.WeightsAsync(_uow, userId, rows.Min(r => r.PhotoDate), rows.Max(r => r.PhotoDate), ct);
        var sessions = rows.GroupBy(r => r.PhotoDate).OrderByDescending(g => g.Key).Select(g =>
        {
            var has = weights.TryGetValue(g.Key, out var w);
            return new ProgressPhotoSessionModel(g.Key,
                g.OrderBy(r => (int)r.Angle).Select(r => ProgressPhotoSupport.ToModel(r, _jwt.Value.SecretKey, _options.Value, now)).ToList(),
                has ? w.Scale : null, has ? w.Trend : null);
        }).ToList();
        return new ProgressPhotoSessionsResponse(consent is not null, sessions);
    }
}
