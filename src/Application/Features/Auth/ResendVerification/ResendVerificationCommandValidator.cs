namespace Application;

public sealed class ResendVerificationCommandValidator : AbstractValidator<ResendVerificationCommand>
{
    public ResendVerificationCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(ValidationMessages.Required)
            .EmailAddress().WithMessage(ValidationMessages.InvalidEmail);
    }
}
