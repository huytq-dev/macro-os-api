namespace Application;

public sealed class CompleteOnboardingCommandValidator : AbstractValidator<CompleteOnboardingCommand>
{
    public CompleteOnboardingCommandValidator()
    {
        RuleFor(x => x.AcceptHealthDisclaimer).Equal(true).WithMessage(ValidationMessages.MustAcceptHealthDisclaimer);
        RuleFor(x => x.AcceptPrivacyNotice).Equal(true).WithMessage(ValidationMessages.MustAcceptPrivacyNotice);
    }
}
