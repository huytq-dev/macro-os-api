using System.Text.Json;

namespace Application;

/// <summary>Quy tắc cho phần "Preparation (optional)" của recipe: link, các bước làm, icon.</summary>
internal static class RecipePreparation
{
    public const int DescriptionMaxLength = 1500;
    public const int LinkMaxLength = 500;
    public const int MaxSteps = 50;
    public const int StepMaxLength = 300;

    public static bool IsHttpUrl(string? text)
        => Uri.TryCreate(text?.Trim(), UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);

    public static void Rules<T>(AbstractValidator<T> v, Expression<Func<T, IEnumerable<RecipeStepInput>>> steps, Expression<Func<T, string?>> iconKey)
    {
        v.RuleFor(steps).Must(s => s is null || s.Count() <= MaxSteps).WithMessage(ValidationMessages.RecipeStepsMax);
        v.RuleForEach(steps).ChildRules(s => s.RuleFor(x => x.Text).NotEmpty().WithMessage(ValidationMessages.Required)
            .MaximumLength(StepMaxLength).WithMessage(ValidationMessages.MaxLength));
        v.RuleFor(iconKey).Must(FoodIconKeys.IsValid).When(x => iconKey.Compile()(x) is not null).WithMessage(ValidationMessages.InvalidIconKey);
    }
}
