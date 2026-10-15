namespace Application;

public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.ExpiredAccessToken)
            .NotEmpty().WithMessage(ValidationMessages.Required);

        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage(ValidationMessages.Required);
    }
}
