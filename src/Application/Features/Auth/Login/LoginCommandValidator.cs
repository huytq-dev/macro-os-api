namespace Application;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(ValidationMessages.Required)
            .EmailAddress().WithMessage(ValidationMessages.InvalidEmail);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage(ValidationMessages.Required);
    }
}
