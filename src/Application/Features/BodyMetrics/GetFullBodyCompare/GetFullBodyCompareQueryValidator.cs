namespace Application;

public sealed class GetFullBodyCompareQueryValidator : AbstractValidator<GetFullBodyCompareQuery>
{
    public GetFullBodyCompareQueryValidator()
    {
        RuleFor(x => x.DateA).NotNull().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.DateB).NotNull().WithMessage(ValidationMessages.Required);
    }
}
