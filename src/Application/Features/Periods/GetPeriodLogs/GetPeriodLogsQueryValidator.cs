namespace Application;

public sealed class GetPeriodLogsQueryValidator : AbstractValidator<GetPeriodLogsQuery>
{
    public const int MaxRangeDays = 3660;

    public GetPeriodLogsQueryValidator()
        => RuleFor(x => x).Must(x => x.From is null || x.To is null || (x.From <= x.To && x.To.Value.DayNumber - x.From.Value.DayNumber + 1 <= MaxRangeDays))
            .WithName("From").WithMessage($"Range must be valid (From ≤ To) and at most {MaxRangeDays} days.");
}
