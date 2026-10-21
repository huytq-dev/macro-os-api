namespace Application;

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
