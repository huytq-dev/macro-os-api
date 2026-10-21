namespace Application;

public sealed class GetNutrientDayDetailQueryValidator : AbstractValidator<GetNutrientDayDetailQuery>
{
    public GetNutrientDayDetailQueryValidator() => RuleFor(x => x.Code).NotEmpty().MaximumLength(64);
}
