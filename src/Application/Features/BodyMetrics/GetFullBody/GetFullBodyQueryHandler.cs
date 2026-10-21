namespace Application;

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
