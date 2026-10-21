namespace Application;

public sealed class CompleteOnboardingCommandValidator : AbstractValidator<CompleteOnboardingCommand>
{
    public CompleteOnboardingCommandValidator()
    {
        RuleFor(x => x.AcceptHealthDisclaimer).Equal(true).WithMessage("Cần đồng ý Health Disclaimer để tiếp tục.");
        RuleFor(x => x.AcceptPrivacyNotice).Equal(true).WithMessage("Cần đồng ý Consumer Health Privacy Notice để tiếp tục.");
    }
}
