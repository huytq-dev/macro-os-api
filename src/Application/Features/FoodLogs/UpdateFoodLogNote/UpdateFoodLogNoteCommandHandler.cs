namespace Application;

public sealed class UpdateFoodLogNoteCommandHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<UpdateFoodLogNoteCommand, Unit>
{
    public async Task<Unit> Handle(UpdateFoodLogNoteCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var note = await _uow.Repository<FoodLogNote>().FindTrackedAsync(n => n.Id == request.Id && n.UserId == userId, ct)
            ?? throw new NotFoundException(ExceptionMessages.NotFound("FoodLogNote", request.Id));

        note.Text = request.Text.Trim();
        note.NotedAt = request.NotedAt ?? note.NotedAt;
        if (request.Name is not null) note.Name = FoodLogNoteRules.CleanName(request.Name);
        note.Color = request.Color ?? note.Color;
        await _uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
