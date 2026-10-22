namespace Application;

public sealed class UpdateGoalTargetCommandValidator : AbstractValidator<UpdateGoalTargetCommand>
{
    public UpdateGoalTargetCommandValidator()
    {
        RuleFor(x => x.TargetWeightKg).InclusiveBetween(20m, 400m).WithMessage(ValidationMessages.NumberRange)
            .When(x => x.TargetWeightKg.HasValue);
    }
}
