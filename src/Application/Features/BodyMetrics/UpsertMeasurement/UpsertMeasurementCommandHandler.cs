namespace Application;

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
