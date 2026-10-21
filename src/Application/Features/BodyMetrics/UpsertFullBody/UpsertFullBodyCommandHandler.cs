namespace Application;

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
