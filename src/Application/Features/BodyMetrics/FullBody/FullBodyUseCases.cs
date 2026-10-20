namespace Application;

/// <summary>Chuyển qua lại giữa hàng Tape của BodyMetric và <see cref="TapeMeasurementsModel"/>; hàm thuần.</summary>
public static class FullBodySupport
{
    public static TapeMeasurementsModel ToModel(BodyMetric m) => new(
        m.NeckCm, m.ShouldersCm, m.BustCm, m.ChestCm, m.WaistCm, m.HipCm, m.LeftArmCm, m.RightArmCm, m.LeftForearmCm, m.RightForearmCm,
        m.LeftWristCm, m.RightWristCm, m.LeftThighCm, m.RightThighCm, m.LeftCalfCm, m.RightCalfCm, m.LeftAnkleCm, m.RightAnkleCm);

    /// <summary>Ghi MỌI ô (null = xoá ô đó): sheet gửi nguyên trạng thái, nên ô user bỏ trống phải được xoá.</summary>
    public static void Apply(BodyMetric m, TapeMeasurementsModel t)
    {
        m.NeckCm = t.NeckCm; m.ShouldersCm = t.ShouldersCm; m.BustCm = t.BustCm; m.ChestCm = t.ChestCm; m.WaistCm = t.WaistCm; m.HipCm = t.HipCm;
        m.LeftArmCm = t.LeftArmCm; m.RightArmCm = t.RightArmCm; m.LeftForearmCm = t.LeftForearmCm; m.RightForearmCm = t.RightForearmCm;
        m.LeftWristCm = t.LeftWristCm; m.RightWristCm = t.RightWristCm; m.LeftThighCm = t.LeftThighCm; m.RightThighCm = t.RightThighCm;
        m.LeftCalfCm = t.LeftCalfCm; m.RightCalfCm = t.RightCalfCm; m.LeftAnkleCm = t.LeftAnkleCm; m.RightAnkleCm = t.RightAnkleCm;
    }

    public static FullBodyResponse Build(DateOnly date, BodyMetric? visual, BodyMetric? tape, decimal? scaleWeightKg = null, decimal? trendWeightKg = null)
    {
        var tapeModel = tape is null ? null : ToModel(tape);
        var hasTape = tapeModel is { HasAnyValue: true };
        return new FullBodyResponse(date, visual?.BodyFatPercent, hasTape ? tapeModel : null, visual?.BodyFatPercent is not null || hasTape, scaleWeightKg, trendWeightKg);
    }

    public static async Task<(BodyMetric? Visual, BodyMetric? Tape)> LoadAsync(IUnitOfWork uow, Guid userId, DateOnly date, bool tracking, CancellationToken ct)
    {
        var rows = await uow.Repository<BodyMetric>().Query(tracking)
            .Where(b => b.UserId == userId && b.MeasuredDate == date && (b.Method == MeasurementMethod.Visual || b.Method == MeasurementMethod.Tape))
            .OrderBy(b => b.CreatedAt)
            .ToListAsync(ct);
        return (rows.FirstOrDefault(r => r.Method == MeasurementMethod.Visual), rows.FirstOrDefault(r => r.Method == MeasurementMethod.Tape));
    }
}

/// <summary>Sheet Metrics (Full Body) của một ngày (mặc định hôm nay) để điền sẵn.</summary>
public sealed record GetFullBodyQuery(DateOnly? Date) : IRequest<FullBodyResponse>;

public sealed class GetFullBodyQueryHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<GetFullBodyQuery, FullBodyResponse>
{
    public async Task<FullBodyResponse> Handle(GetFullBodyQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var date = request.Date ?? (await UserLocale.GetAsync(_uow, userId, _timeProvider, ct)).Today;
        var (visual, tape) = await FullBodySupport.LoadAsync(_uow, userId, date, tracking: false, ct);
        var weights = await FullBodyWriter.WeightsAsync(_uow, userId, date, date, ct);
        return FullBodySupport.Build(date, visual, tape, weights.TryGetValue(date, out var w) ? w.Scale : null, weights.TryGetValue(date, out w) ? w.Trend : null);
    }
}

/// <summary>
/// Lưu cả sheet trong MỘT lần lưu (một transaction), thay thế trạng thái của ngày: <paramref name="VisualBodyFatPercent"/> có giá trị → hàng Visual,
/// null → xoá hàng Visual; <paramref name="Tape"/> có ít nhất một ô → hàng Tape (ô null bị xoá), không có → xoá hàng Tape. Idempotent.
/// </summary>
public sealed record UpsertFullBodyCommand(DateOnly? Date, decimal? VisualBodyFatPercent, TapeMeasurementsModel? Tape) : IRequest<FullBodyResponse>;

public sealed class UpsertFullBodyCommandValidator : AbstractValidator<UpsertFullBodyCommand>
{
    public UpsertFullBodyCommandValidator(TimeProvider timeProvider)
    {
        RuleFor(x => x.Date)
            .LessThanOrEqualTo(_ => DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime.AddDays(1)))
            .WithMessage(ValidationMessages.NotInFuture)
            .When(x => x.Date.HasValue);
        RuleFor(x => x.VisualBodyFatPercent)
            .InclusiveBetween(BodyMeasurements.VisualBodyFatMin, BodyMeasurements.VisualBodyFatMax).WithMessage(ValidationMessages.Range)
            .When(x => x.VisualBodyFatPercent.HasValue);

        // Mỗi ô số đo (nếu nhập) trong khoảng hợp lý của chính số đo đó (BodyMeasurements.All)
        foreach (var m in BodyMeasurements.All)
            RuleFor(x => x.Tape).Must(t => m.FromModel(t!) is null || (m.FromModel(t!) >= m.Min && m.FromModel(t!) <= m.Max))
                .WithMessage(ValidationMessages.TapeMeasurementRange(m.ModelName, m.Min, m.Max))
                .When(x => x.Tape is not null);
    }
}

public sealed class UpsertFullBodyCommandHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<UpsertFullBodyCommand, FullBodyResponse>
{
    public async Task<FullBodyResponse> Handle(UpsertFullBodyCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var locale = await UserLocale.GetAsync(_uow, userId, _timeProvider, ct);
        var date = request.Date ?? locale.Today;
        locale.EnsureNotFuture(date, "Date");
        var repo = _uow.Repository<BodyMetric>();
        var (visual, tape) = await FullBodySupport.LoadAsync(_uow, userId, date, tracking: true, ct);

        if (request.VisualBodyFatPercent is { } percent)
        {
            if (visual is null)
            {
                visual = new BodyMetric { Id = Guid.NewGuid(), UserId = userId, MeasuredDate = date, Method = MeasurementMethod.Visual };
                await repo.AddAsync(visual, ct);
            }
            visual.BodyFatPercent = percent;
        }
        else if (visual is not null)
        {
            await repo.RemoveAsync(visual, ct); // interceptor chuyển thành soft delete
            visual = null;
        }

        if (request.Tape is { HasAnyValue: true } t)
        {
            if (tape is null)
            {
                tape = new BodyMetric { Id = Guid.NewGuid(), UserId = userId, MeasuredDate = date, Method = MeasurementMethod.Tape };
                await repo.AddAsync(tape, ct);
            }
            FullBodySupport.Apply(tape, t);
        }
        else if (tape is not null)
        {
            await repo.RemoveAsync(tape, ct);
            tape = null;
        }

        await _uow.EnsureSaveAsync(ct);
        var weights = await FullBodyWriter.WeightsAsync(_uow, userId, date, date, ct);
        return FullBodySupport.Build(date, visual, tape, weights.TryGetValue(date, out var w) ? w.Scale : null, weights.TryGetValue(date, out w) ? w.Trend : null);
    }
}

/// <summary>Xoá sheet Metrics của một ngày (mặc định hôm nay): cả hàng Visual lẫn Tape (soft delete).</summary>
public sealed record DeleteFullBodyCommand(DateOnly? Date) : IRequest<Unit>;

public sealed class DeleteFullBodyCommandHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<DeleteFullBodyCommand, Unit>
{
    public async Task<Unit> Handle(DeleteFullBodyCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var date = request.Date ?? (await UserLocale.GetAsync(_uow, userId, _timeProvider, ct)).Today;
        var (visual, tape) = await FullBodySupport.LoadAsync(_uow, userId, date, tracking: true, ct);
        foreach (var row in new[] { visual, tape }.OfType<BodyMetric>())
            await _uow.Repository<BodyMetric>().RemoveAsync(row, ct);
        await _uow.EnsureSaveAsync(ct);
        return Unit.Value;
    }
}
