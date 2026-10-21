namespace Application;

public sealed class GetOnboardingStateQueryHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<GetOnboardingStateQuery, OnboardingStateResponse>
{
    public async Task<OnboardingStateResponse> Handle(GetOnboardingStateQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var profile = await _uow.Repository<UserProfile>().FindAsync(p => p.UserId == userId, ct)
            ?? throw new NotFoundException(ExceptionMessages.NotFound("UserProfile", userId));

        var weight = await _uow.Repository<WeightEntry>().Query()
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.LogDate)
            .Select(w => (decimal?)w.WeightKg)
            .FirstOrDefaultAsync(ct);
        var hasProgram = await _uow.Repository<MacroProgram>().Query().AnyAsync(p => p.UserId == userId && p.IsActive, ct);
        var consent = profile.HealthDisclaimerAcceptedAt is not null && profile.PrivacyNoticeAcceptedAt is not null;

        return new OnboardingStateResponse(
            profile.OnboardingState.ToString(), profile.OnboardingStep,
            IsRequired: !hasProgram || !consent,
            hasProgram, consent,
            // Chưa trả lời giới tính thì để null (cột không cho null nên dùng trạng thái InProgress để biết đã trả lời)
            profile.OnboardingState == OnboardingState.NotStarted ? null : profile.Gender.ToString(),
            profile.DateOfBirth, profile.HeightCm, weight, profile.UnitPreference.ToString(),
            profile.WeighedMoreThanBefore?.ToString(), profile.RecentWeightTrend?.ToString(), profile.BodyFat?.ToString(),
            profile.ExerciseSessions?.ToString(), profile.DailyActivity?.ToString(),
            profile.LiftingExperience?.ToString(), profile.CardioExperience?.ToString());
    }
}
