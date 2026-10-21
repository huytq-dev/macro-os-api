namespace Application;

public sealed class GetEnergyBalanceSeriesQueryValidator : AbstractValidator<GetEnergyBalanceSeriesQuery>
{
    public GetEnergyBalanceSeriesQueryValidator()
        => RuleFor(x => x).Must(x => x.From is null || x.To is null || x.From <= x.To).WithName("From").WithMessage("From must not be after To.");
}
