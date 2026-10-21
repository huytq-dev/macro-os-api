namespace Application;

public sealed class UpdateNutrientTargetCommandValidator : AbstractValidator<UpdateNutrientTargetCommand>
{
    public UpdateNutrientTargetCommandValidator()
    {
        RuleFor(x => x.Floor).GreaterThanOrEqualTo(0).When(x => x.Floor.HasValue);
        RuleFor(x => x.Target).GreaterThanOrEqualTo(0).When(x => x.Target.HasValue);
        RuleFor(x => x.Ceiling).GreaterThanOrEqualTo(0).When(x => x.Ceiling.HasValue);
        RuleFor(x => x).Must(x =>
                (x.Floor is null || x.Target is null || x.Floor <= x.Target)
                && (x.Target is null || x.Ceiling is null || x.Target <= x.Ceiling)
                && (x.Floor is null || x.Ceiling is null || x.Floor <= x.Ceiling))
            .WithMessage("Phải đúng thứ tự Floor ≤ Target ≤ Ceiling.");
    }
}
