namespace Application;

public sealed record DeleteBodyMetricCommand(Guid Id) : IRequest<Unit>;

public sealed class DeleteBodyMetricCommandHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<DeleteBodyMetricCommand, Unit>
{
    public async Task<Unit> Handle(DeleteBodyMetricCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var row = await _uow.Repository<BodyMetric>().FindTrackedAsync(b => b.Id == request.Id && b.UserId == userId, ct)
            ?? throw new NotFoundException(ExceptionMessages.NotFound("BodyMetric", request.Id));

        await _uow.Repository<BodyMetric>().RemoveAsync(row, ct); // interceptor chuyển thành soft delete
        await _uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
