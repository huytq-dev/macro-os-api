namespace Application;

public sealed class GetWeighInHabitQueryValidator : AbstractValidator<GetWeighInHabitQuery>
{
    public GetWeighInHabitQueryValidator()
    {
        RuleFor(x => x.Year!.Value).InclusiveBetween(2000, DateRuleBuilderExtensions.UtcToday.Year + 1).WithMessage(ValidationMessages.NumberRange).When(x => x.Year.HasValue);
        RuleFor(x => x.Month!.Value).InclusiveBetween(1, 12).WithMessage(ValidationMessages.NumberRange).When(x => x.Month.HasValue);
    }
}
