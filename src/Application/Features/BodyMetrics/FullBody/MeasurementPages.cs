namespace Application;

/// <summary>
/// Trang riêng của MỘT số đo vòng (Neck, Waist, Left Bicep…; khoá trong <see cref="BodyMeasurements.All"/>): các lần đo trong khoảng (mới nhất trước, mỗi ngày một giá trị, cm),
/// thống kê khoảng (Average, Difference = cuối − đầu) và bucket Day/Week/Month — cùng luật khoảng như GET /weights. Dữ liệu là cột của hàng Tape: chung nguồn với Full Body.
/// <c>Limit</c> chỉ cắt danh sách <c>Items</c> (Summary/Buckets vẫn tính trên cả khoảng), dùng cho thẻ Dashboard "last 7 entries".
/// </summary>
public sealed record GetMeasurementSeriesQuery(string Key, DateOnly? From, DateOnly? To, MetricGranularity? Granularity = null, bool? All = null, int? Limit = null)
    : IRequest<MeasurementSeriesResponse>;

public sealed class GetMeasurementSeriesQueryHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<GetMeasurementSeriesQuery, MeasurementSeriesResponse>
{
    public async Task<MeasurementSeriesResponse> Handle(GetMeasurementSeriesQuery request, CancellationToken ct)
    {
        var m = BodyMeasurements.Find(request.Key) ?? throw new NotFoundException(ExceptionMessages.NotFound("Measurement", request.Key));
        var userId = _userContext.UserId;
        var locale = await UserLocale.GetAsync(_uow, userId, _timeProvider, ct);
        var to = request.To ?? locale.Today;
        var from = request.From ?? (request.All == true ? DateOnly.MinValue : to.AddDays(-90));

        var rows = await _uow.Repository<BodyMetric>().Query()
            .Where(b => b.UserId == userId && b.Method == MeasurementMethod.Tape && b.MeasuredDate >= from && b.MeasuredDate <= to)
            .OrderByDescending(b => b.MeasuredDate).ThenByDescending(b => b.CreatedAt)
            .ToListAsync(ct);

        // Mỗi ngày một giá trị (dữ liệu cũ có thể trùng ngày: lấy hàng mới nhất có giá trị)
        var all = rows.Where(r => m.Get(r) is not null).GroupBy(r => r.MeasuredDate)
            .Select(g => new MeasurementEntryModel(g.Key, m.Get(g.First())!.Value)).ToList();

        if (request.From is null && request.All == true) from = all.Count == 0 ? to : all[^1].Date;
        var (summary, buckets) = MetricSeries.Build(all.Select(i => (i.Date, i.ValueCm)), from, to, request.Granularity, locale.WeekStart);
        var items = request.Limit is { } limit ? all.Take(Math.Clamp(limit, 1, 1000)).ToList() : all;
        return new MeasurementSeriesResponse(m.Key, m.Group, items, summary, buckets, all.FirstOrDefault()?.ValueCm);
    }
}

/// <summary>Ghi MỘT số đo của một ngày (cm; Date null = hôm nay theo múi giờ user, không ở tương lai); ngày đã có giá trị thì ghi đè. Các số đo khác của ngày đó không đổi.</summary>
public sealed record UpsertMeasurementCommand(
    // Nullable: route gán Key sau khi bind body; non-nullable thì model validation báo "Key field is required" trước controller.
    [property: System.Text.Json.Serialization.JsonIgnore] string? Key,
    DateOnly? Date,
    decimal ValueCm) : IRequest<MeasurementEntryModel>;

public sealed class UpsertMeasurementCommandValidator : AbstractValidator<UpsertMeasurementCommand>
{
    public UpsertMeasurementCommandValidator()
    {
        RuleFor(x => x.Key).Must(k => BodyMeasurements.Find(k) is not null).WithMessage(ValidationMessages.UnknownMeasurementKey);
        RuleFor(x => x).Must(x => BodyMeasurements.Find(x.Key) is not { } m || (x.ValueCm >= m.Min && x.ValueCm <= m.Max))
            .WithName("ValueCm")
            .WithMessage(x => BodyMeasurements.Find(x.Key) is { } m ? ValidationMessages.TapeMeasurementRange(m.ModelName, m.Min, m.Max) : ValidationMessages.UnknownMeasurementKey);
        RuleFor(x => x.Date)
            .LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)))
            .WithMessage(ValidationMessages.NotInFuture)
            .When(x => x.Date.HasValue);
    }
}

public sealed class UpsertMeasurementCommandHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<UpsertMeasurementCommand, MeasurementEntryModel>
{
    public async Task<MeasurementEntryModel> Handle(UpsertMeasurementCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var m = BodyMeasurements.Find(request.Key)!;
        var locale = await UserLocale.GetAsync(_uow, userId, _timeProvider, ct);
        var date = request.Date ?? locale.Today;
        locale.EnsureNotFuture(date, "Date");

        await FullBodyWriter.ApplyAsync(_uow, userId, date, new Dictionary<string, decimal?> { [m.Key] = request.ValueCm }, ct);
        await _uow.EnsureSaveAsync(ct);
        return new MeasurementEntryModel(date, request.ValueCm);
    }
}

/// <summary>Xoá MỘT số đo của một ngày (các số đo khác của ngày đó giữ nguyên); không có giá trị → 404.</summary>
public sealed record DeleteMeasurementCommand(string Key, DateOnly Date) : IRequest<Unit>;

public sealed class DeleteMeasurementCommandHandler(IUnitOfWork _uow, IUserContext _userContext) : IRequestHandler<DeleteMeasurementCommand, Unit>
{
    public async Task<Unit> Handle(DeleteMeasurementCommand request, CancellationToken ct)
    {
        var m = BodyMeasurements.Find(request.Key) ?? throw new NotFoundException(ExceptionMessages.NotFound("Measurement", request.Key));
        var (_, _, changed) = await FullBodyWriter.ApplyAsync(_uow, _userContext.UserId, request.Date, new Dictionary<string, decimal?> { [m.Key] = null }, ct);
        if (!changed) throw new NotFoundException(ExceptionMessages.NotFoundField("Measurement", "Date", request.Date));
        await _uow.EnsureSaveAsync(ct);
        return Unit.Value;
    }
}

/// <summary>
/// Dashboard: danh sách các số đo vòng ĐÃ có dữ liệu, mỗi số đo kèm giá trị mới nhất và <c>Entries</c> lần đo gần nhất (mặc định 7, tối đa 30; mới nhất trước, mỗi ngày một giá trị).
/// Số đo chưa từng có dữ liệu không xuất hiện. Cân nặng và % mỡ Visual có API riêng (GET /weights?limit=7, GET /body-metrics/visual-body-fat?limit=7).
/// </summary>
public sealed record GetMeasurementOverviewQuery(int? Entries = null) : IRequest<MeasurementOverviewResponse>;

public sealed class GetMeasurementOverviewQueryHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<GetMeasurementOverviewQuery, MeasurementOverviewResponse>
{
    public async Task<MeasurementOverviewResponse> Handle(GetMeasurementOverviewQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var count = Math.Clamp(request.Entries ?? 7, 1, 30);
        var rows = await _uow.Repository<BodyMetric>().Query()
            .Where(b => b.UserId == userId && b.Method == MeasurementMethod.Tape)
            .OrderByDescending(b => b.MeasuredDate).ThenByDescending(b => b.CreatedAt)
            .ToListAsync(ct);

        var metrics = new List<MeasurementOverviewItem>();
        foreach (var m in BodyMeasurements.All)
        {
            var entries = rows.Where(r => m.Get(r) is not null).GroupBy(r => r.MeasuredDate)
                .Select(g => new MeasurementEntryModel(g.Key, m.Get(g.First())!.Value)).Take(count).ToList();
            if (entries.Count > 0) metrics.Add(new MeasurementOverviewItem(m.Key, m.Group, entries[0].Date, entries[0].ValueCm, entries));
        }
        return new MeasurementOverviewResponse(metrics);
    }
}
