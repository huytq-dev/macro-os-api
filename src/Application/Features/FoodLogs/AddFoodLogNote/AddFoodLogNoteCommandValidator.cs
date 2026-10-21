namespace Application;

public sealed class AddFoodLogNoteCommandValidator : AbstractValidator<AddFoodLogNoteCommand>
{
    public AddFoodLogNoteCommandValidator()
    {
        FoodLogNoteRules.Apply(this, x => x.Text, x => x.Name, x => x.Color);
        RuleFor(x => x.LogDate)
            .LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)))
            .WithMessage(ValidationMessages.NotInFuture)
            .When(x => x.LogDate.HasValue);
    }
}
