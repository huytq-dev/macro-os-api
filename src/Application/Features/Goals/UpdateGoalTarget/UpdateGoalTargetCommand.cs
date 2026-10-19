namespace Application;

/// <summary>UC-B08: đặt/xoá cân nặng mục tiêu của goal đang hoạt động (null = xoá target).</summary>
public sealed record UpdateGoalTargetCommand(decimal? TargetWeightKg) : IRequest<Unit>;

public sealed class UpdateGoalTargetCommandValidator : AbstractValidator<UpdateGoalTargetCommand>
{
    public UpdateGoalTargetCommandValidator()
    {
        RuleFor(x => x.TargetWeightKg).InclusiveBetween(20m, 400m).WithMessage(ValidationMessages.Range)
            .When(x => x.TargetWeightKg.HasValue);
    }
}

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
