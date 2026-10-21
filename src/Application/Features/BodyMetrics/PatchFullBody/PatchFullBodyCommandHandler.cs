namespace Application;

public sealed class PatchFullBodyCommandHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<PatchFullBodyCommand, FullBodyResponse>
{
    public async Task<FullBodyResponse> Handle(PatchFullBodyCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var locale = await UserLocale.GetAsync(_uow, userId, _timeProvider, ct);
        var date = request.Date ?? locale.Today;
        locale.EnsureNotFuture(date, "Date");

        var (visual, tape, _) = await FullBodyWriter.ApplyAsync(_uow, userId, date, request.Changes(), ct);
        await _uow.EnsureSaveAsync(ct);

        var weights = await FullBodyWriter.WeightsAsync(_uow, userId, date, date, ct);
        return FullBodySupport.Build(date, visual, tape, weights.TryGetValue(date, out var w) ? w.Scale : null, weights.TryGetValue(date, out w) ? w.Trend : null);
    }
}
