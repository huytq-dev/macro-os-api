namespace Application;

public sealed class UpdateFoodLogNoteCommandValidator : AbstractValidator<UpdateFoodLogNoteCommand>
{
    public UpdateFoodLogNoteCommandValidator() => FoodLogNoteRules.Apply(this, x => x.Text, x => x.Name, x => x.Color);
}
