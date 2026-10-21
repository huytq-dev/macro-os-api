namespace Application;

public sealed class DeleteFoodLogNoteCommandHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<DeleteFoodLogNoteCommand, Unit>
{
    public async Task<Unit> Handle(DeleteFoodLogNoteCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var note = await _uow.Repository<FoodLogNote>().FindTrackedAsync(n => n.Id == request.Id && n.UserId == userId, ct)
            ?? throw new NotFoundException(ExceptionMessages.NotFound("FoodLogNote", request.Id));

        await _uow.Repository<FoodLogNote>().RemoveAsync(note, ct);
        await _uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
