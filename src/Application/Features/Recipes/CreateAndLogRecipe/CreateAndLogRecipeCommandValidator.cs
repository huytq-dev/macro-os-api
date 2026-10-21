using System.Text.Json;

namespace Application;

public sealed class CreateAndLogRecipeCommandValidator : AbstractValidator<CreateAndLogRecipeCommand>
{
    public CreateAndLogRecipeCommandValidator()
    {
        RuleFor(x => x.Recipe).NotNull().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.Log!.Quantity).InclusiveBetween(0.01m, 10000).When(x => x.Log is not null);
    }
}
