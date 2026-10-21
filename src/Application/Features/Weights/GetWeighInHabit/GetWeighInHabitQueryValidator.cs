namespace Application;

public sealed class GetWeighInHabitQueryValidator : AbstractValidator<GetWeighInHabitQuery>
{
    public GetWeighInHabitQueryValidator()
    {
        RuleFor(x => x.Year!.Value).InclusiveBetween(2000, DateTime.UtcNow.Year + 1).WithMessage(ValidationMessages.Range).When(x => x.Year.HasValue);
        RuleFor(x => x.Month!.Value).InclusiveBetween(1, 12).WithMessage(ValidationMessages.Range).When(x => x.Month.HasValue);
    }
}
