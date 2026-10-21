namespace Application;

public sealed class GetFoodSuggestionsQueryValidator : AbstractValidator<GetFoodSuggestionsQuery>
{
    public GetFoodSuggestionsQueryValidator()
    {
        RuleFor(x => x.Top).InclusiveBetween(1, 30);
        RuleFor(x => x.Weekday).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue).When(x => x.Weekday.HasValue);
    }
}
