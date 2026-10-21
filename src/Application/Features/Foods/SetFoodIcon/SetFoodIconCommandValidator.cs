namespace Application;

public sealed class SetFoodIconCommandValidator : AbstractValidator<SetFoodIconCommand>
{
    public SetFoodIconCommandValidator()
        => RuleFor(x => x.IconKey).Must(FoodIconKeys.IsValid).When(x => x.IconKey is not null).WithMessage(ValidationMessages.InvalidIconKey);
}
