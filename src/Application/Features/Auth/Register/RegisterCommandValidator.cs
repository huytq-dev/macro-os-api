namespace Application;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(ValidationMessages.Required)
            .EmailAddress().WithMessage(ValidationMessages.InvalidEmail)
            .MaximumLength(320).WithMessage(ValidationMessages.MaxLength);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage(ValidationMessages.Required)
            .MinimumLength(8).WithMessage(ValidationMessages.MinLength)
            .MaximumLength(100).WithMessage(ValidationMessages.MaxLength);

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage(ValidationMessages.Required)
            .Equal(x => x.Password).WithMessage(ValidationMessages.MustMatch);

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage(ValidationMessages.Required)
            .MaximumLength(100).WithMessage(ValidationMessages.MaxLength);

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage(ValidationMessages.Required)
            .MaximumLength(100).WithMessage(ValidationMessages.MaxLength);

        RuleFor(x => x.Gender)
            .IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);
    }
}
