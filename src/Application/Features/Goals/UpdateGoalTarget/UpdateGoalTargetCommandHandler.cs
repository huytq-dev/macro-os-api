namespace Application;

public sealed class UpdateGoalTargetCommandHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<UpdateGoalTargetCommand, Unit>
{
    public async Task<Unit> Handle(UpdateGoalTargetCommand request, CancellationToken ct)
    {
        var goal = await GoalSupport.EnsureActiveGoalAsync(_uow, _userContext.UserId, ct)
            ?? throw new NotFoundException(ExceptionMessages.NoActiveGoal);

        goal.TargetWeightKg = request.TargetWeightKg;
        await _uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
