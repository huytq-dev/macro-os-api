namespace Application;

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
