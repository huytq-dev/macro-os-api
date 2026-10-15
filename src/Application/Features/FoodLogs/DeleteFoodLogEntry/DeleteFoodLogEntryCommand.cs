namespace Application;

public sealed record DeleteFoodLogEntryCommand(Guid Id) : IRequest<Unit>;

public sealed class DeleteFoodLogEntryCommandHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<DeleteFoodLogEntryCommand, Unit>
{
    public async Task<Unit> Handle(DeleteFoodLogEntryCommand request, CancellationToken ct)
    {
        var entry = await FoodLogSupport.GetOwnedEntryAsync(_uow, _userContext.UserId, request.Id, ct);
        await _uow.Repository<FoodLogEntry>().RemoveAsync(entry, ct); // soft delete qua interceptor
        entry.IsDeleted = true; // để RecalcTotals bỏ qua ngay trong cùng unit of work
        FoodLogSupport.RecalcTotals(entry.FoodLog);
        await _uow.EnsureSaveAsync(ct);
        return Unit.Value;
    }
}

/// <summary>UC-D07: xoá cả ngày (hard delete FoodLog, cascade entries) → thuật toán coi ngày này là chưa log.</summary>
public sealed record ClearDayLogCommand(DateOnly Date) : IRequest<Unit>;

public sealed class ClearDayLogCommandHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<ClearDayLogCommand, Unit>
{
    public async Task<Unit> Handle(ClearDayLogCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var log = await _uow.Repository<FoodLog>().FindTrackedAsync(l => l.UserId == userId && l.LogDate == request.Date, ct)
            ?? throw new NotFoundException(ExceptionMessages.NotFound("FoodLog", request.Date));
        await _uow.Repository<FoodLog>().RemoveAsync(log, ct);
        await _uow.EnsureSaveAsync(ct);
        return Unit.Value;
    }
}
