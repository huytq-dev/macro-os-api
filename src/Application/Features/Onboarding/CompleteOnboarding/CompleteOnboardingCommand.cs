namespace Application;

/// <summary>Hoàn tất wizard: cần program đã tạo và đồng ý CẢ HAI điều khoản (Health Disclaimer, Consumer Health Privacy Notice).</summary>
public sealed record CompleteOnboardingCommand(bool AcceptHealthDisclaimer, bool AcceptPrivacyNotice) : IRequest<Unit>;

public sealed class CompleteOnboardingCommandValidator : AbstractValidator<CompleteOnboardingCommand>
{
    public CompleteOnboardingCommandValidator()
    {
        RuleFor(x => x.AcceptHealthDisclaimer).Equal(true).WithMessage("Cần đồng ý Health Disclaimer để tiếp tục.");
        RuleFor(x => x.AcceptPrivacyNotice).Equal(true).WithMessage("Cần đồng ý Consumer Health Privacy Notice để tiếp tục.");
    }
}

public sealed class CompleteOnboardingCommandHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<CompleteOnboardingCommand, Unit>
{
    public async Task<Unit> Handle(CompleteOnboardingCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        if (!await _uow.Repository<MacroProgram>().Query().AnyAsync(p => p.UserId == userId && p.IsActive, ct))
            throw new BadRequestException("Cần tạo program trước khi hoàn tất thiết lập.");

        var profile = await _uow.Repository<UserProfile>().FindTrackedAsync(p => p.UserId == userId, ct)
            ?? throw new NotFoundException(ExceptionMessages.NotFound("UserProfile", userId));

        var now = _timeProvider.GetUtcNow();
        profile.HealthDisclaimerAcceptedAt ??= now;
        profile.PrivacyNoticeAcceptedAt ??= now;
        profile.OnboardingState = OnboardingState.Completed;

        await _uow.EnsureSaveAsync(ct);
        return Unit.Value;
    }
}
