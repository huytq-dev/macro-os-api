namespace Application;

public sealed class SaveOnboardingBasicsCommandHandler(IUnitOfWork _uow, IUserContext _userContext, ISender _sender)
    : IRequestHandler<SaveOnboardingBasicsCommand, Unit>
{
    public async Task<Unit> Handle(SaveOnboardingBasicsCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var profile = await _uow.Repository<UserProfile>().FindTrackedAsync(p => p.UserId == userId, ct)
            ?? throw new NotFoundException(ExceptionMessages.NotFound("UserProfile", userId));

        if (request.Gender is { } gender) profile.Gender = gender;
        if (request.DateOfBirth is { } dob) profile.DateOfBirth = dob;
        if (request.HeightCm is { } height) profile.HeightCm = height;
        if (request.UnitPreference is { } unit) profile.UnitPreference = unit;
        if (request.WeighedMoreThanBefore is { } history) profile.WeighedMoreThanBefore = history;
        if (request.RecentWeightTrend is { } trend) profile.RecentWeightTrend = trend;
        if (request.BodyFat is { } bodyFat) profile.BodyFat = bodyFat;
        if (request.ExerciseSessions is { } sessions) profile.ExerciseSessions = sessions;
        if (request.DailyActivity is { } daily) profile.DailyActivity = daily;
        if (request.LiftingExperience is { } lifting) profile.LiftingExperience = lifting;
        if (request.CardioExperience is { } cardio) profile.CardioExperience = cardio;
        if (request.ReferralSource is not null) profile.ReferralSource = request.ReferralSource.Trim();
        if (request.AffiliateCode is not null) profile.AffiliateCode = request.AffiliateCode.Trim();

        // Có đủ hai câu hoạt động thì quy ra ActivityLevel dùng cho TDEE; kinh nghiệm tạ/cardio quy ra TrainingType
        if (profile.DailyActivity is { } d && profile.ExerciseSessions is { } s)
            profile.ActivityLevel = OnboardingCalculator.ActivityFrom(d, s);
        if (request.LiftingExperience.HasValue || request.CardioExperience.HasValue)
            profile.TrainingType = OnboardingCalculator.TrainingFrom(profile.LiftingExperience, profile.CardioExperience);

        if (profile.OnboardingState is OnboardingState.NotStarted or OnboardingState.Skipped)
            profile.OnboardingState = OnboardingState.InProgress;
        profile.OnboardingStep = Math.Max(profile.OnboardingStep, request.Step);

        // Lưu lại đúng giá trị cũ (quay lại bước, tải lại trang) là hợp lệ: không có dòng nào đổi cũng không phải lỗi
        await _uow.SaveChangesAsync(ct);

        if (request.WeightKg is { } weight)
            await _sender.Send(new UpsertWeightEntryCommand(null, weight, null), ct);

        return Unit.Value;
    }
}
