namespace Application;

public sealed class AddFavoriteCommandValidator : AbstractValidator<AddFavoriteCommand>
{
    public AddFavoriteCommandValidator()
    {
        RuleFor(x => x.FoodItemId).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.Quantity).InclusiveBetween(0.01m, 10000);
    }
}
