namespace Application;

/// <summary>
/// Trang Visual Body Fat: các lần ghi % mỡ Visual trong khoảng (mới nhất trước, mỗi ngày một dòng), thống kê khoảng (Average, Difference = cuối − đầu) và bucket Day/Week/Month.
/// Tách biệt với % mỡ đo bằng cân (Method Scale, nằm trong cân nặng). Khoảng: <c>To</c> mặc định hôm nay theo múi giờ user; <c>From</c> mặc định 90 ngày trước <c>To</c>, hoặc ngày có dữ liệu đầu tiên khi <c>All</c> = true.
/// </summary>
public sealed record GetVisualBodyFatQuery(DateOnly? From, DateOnly? To, MetricGranularity? Granularity = null, bool? All = null, int? Limit = null) : IRequest<VisualBodyFatResponse>;

public sealed class GetVisualBodyFatQueryHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<GetVisualBodyFatQuery, VisualBodyFatResponse>
{
    public async Task<VisualBodyFatResponse> Handle(GetVisualBodyFatQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var locale = await UserLocale.GetAsync(_uow, userId, _timeProvider, ct);
        var to = request.To ?? locale.Today;
        var from = request.From ?? (request.All == true ? DateOnly.MinValue : to.AddDays(-90));

        var rows = await _uow.Repository<BodyMetric>().Query()
            .Where(b => b.UserId == userId && b.Method == MeasurementMethod.Visual && b.BodyFatPercent != null && b.MeasuredDate >= from && b.MeasuredDate <= to)
            .OrderByDescending(b => b.MeasuredDate).ThenByDescending(b => b.CreatedAt)
            .Select(b => new { b.Id, b.MeasuredDate, Percent = b.BodyFatPercent!.Value })
            .ToListAsync(ct);

        // Mỗi ngày một dòng (dữ liệu cũ có thể trùng ngày: lấy lần ghi mới nhất)
        var items = rows.GroupBy(r => r.MeasuredDate).Select(g => g.First())
            .Select(r => new VisualBodyFatEntryModel(r.Id, r.MeasuredDate, r.Percent)).ToList();

        if (request.From is null && request.All == true) from = items.Count == 0 ? to : items[^1].Date;
        var (summary, buckets) = MetricSeries.Build(items.Select(i => (i.Date, i.Percent)), from, to, request.Granularity, locale.WeekStart);
        var shown = request.Limit is { } limit ? items.Take(Math.Clamp(limit, 1, 1000)).ToList() : items;
        return new VisualBodyFatResponse(shown, summary, buckets, items.FirstOrDefault()?.Percent);
    }
}

/// <summary>Ghi % mỡ Visual của một ngày (Date null = hôm nay theo múi giờ user, không được ở tương lai); ngày đã có thì ghi đè (mỗi ngày một dòng).</summary>
public sealed record UpsertVisualBodyFatCommand(DateOnly? Date, decimal Percent) : IRequest<Guid>;

public sealed class UpsertVisualBodyFatCommandValidator : AbstractValidator<UpsertVisualBodyFatCommand>
{
    public UpsertVisualBodyFatCommandValidator() => RuleFor(x => x.Percent).InclusiveBetween(2m, 70m).WithMessage(ValidationMessages.Range);
}

public sealed class UpsertVisualBodyFatCommandHandler(ISender _sender) : IRequestHandler<UpsertVisualBodyFatCommand, Guid>
{
    public Task<Guid> Handle(UpsertVisualBodyFatCommand request, CancellationToken ct)
        => _sender.Send(new UpsertBodyFatCommand(request.Date, MeasurementMethod.Visual, request.Percent, null), ct);
}

/// <summary>Xoá % mỡ Visual của một ngày (soft delete); không có → 404.</summary>
public sealed record DeleteVisualBodyFatCommand(DateOnly Date) : IRequest<Unit>;

public sealed class DeleteVisualBodyFatCommandHandler(IUnitOfWork _uow, IUserContext _userContext) : IRequestHandler<DeleteVisualBodyFatCommand, Unit>
{
    public async Task<Unit> Handle(DeleteVisualBodyFatCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var rows = await _uow.Repository<BodyMetric>().Query(tracking: true)
            .Where(b => b.UserId == userId && b.Method == MeasurementMethod.Visual && b.MeasuredDate == request.Date && b.BodyFatPercent != null)
            .ToListAsync(ct);
        if (rows.Count == 0) throw new NotFoundException(ExceptionMessages.NotFoundField("VisualBodyFat", "Date", request.Date));

        foreach (var row in rows) await _uow.Repository<BodyMetric>().RemoveAsync(row, ct); // interceptor chuyển thành soft delete
        await _uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
