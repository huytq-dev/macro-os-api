namespace Application;

public sealed class CompleteOnboardingCommandHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<CompleteOnboardingCommand, Unit>
{
    public async Task<Unit> Handle(CompleteOnboardingCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        if (!await _uow.Repository<MacroProgram>().Query().AnyAsync(p => p.UserId == userId && p.IsActive, ct))
            throw new BadRequestException(ExceptionMessages.OnboardingNeedsProgram);

        var profile = await _uow.Repository<UserProfile>().FindTrackedAsync(p => p.UserId == userId, ct)
            ?? throw new NotFoundException(ExceptionMessages.NotFound("UserProfile", userId));

        var now = _timeProvider.GetUtcNow();
        profile.HealthDisclaimerAcceptedAt ??= now;
        profile.PrivacyNoticeAcceptedAt ??= now;
        profile.OnboardingState = OnboardingState.Completed;

        // Bấm hoàn tất lần nữa khi đã hoàn tất là hợp lệ: không có dòng nào đổi cũng không phải lỗi
        await _uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
