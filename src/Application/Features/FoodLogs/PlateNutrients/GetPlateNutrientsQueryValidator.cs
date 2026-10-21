namespace Application;

public sealed class GetPlateNutrientsQueryValidator : AbstractValidator<GetPlateNutrientsQuery>
{
    public const int MaxLines = 50;

    public GetPlateNutrientsQueryValidator()
    {
        RuleFor(x => x.Items).Must(i => i is null || i.Count <= MaxLines).WithMessage($"At most {MaxLines} items");
        RuleForEach(x => x.Items).SetValidator(new PlateLineValidator());
    }
}
