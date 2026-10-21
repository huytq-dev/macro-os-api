namespace Application;

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
