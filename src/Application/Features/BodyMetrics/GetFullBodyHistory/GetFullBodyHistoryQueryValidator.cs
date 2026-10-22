namespace Application;

public sealed class GetFullBodyHistoryQueryValidator : AbstractValidator<GetFullBodyHistoryQuery>
{
    public GetFullBodyHistoryQueryValidator()
        => RuleFor(x => x).Must(x => x.From is null || x.To is null || x.From <= x.To).WithName("From").WithMessage(ValidationMessages.FromAfterTo);
}
