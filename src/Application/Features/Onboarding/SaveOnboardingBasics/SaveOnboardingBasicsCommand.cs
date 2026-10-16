namespace Application;

/// <summary>
/// Lưu đáp án màn Basics từng bước (một phần, trường nào gửi mới cập nhật). `Step` = số bước đã hoàn thành.
/// Có WeightKg thì ghi cân nặng hôm nay (WeightEntry đầu tiên, trend khởi điểm).
/// </summary>
public sealed record SaveOnboardingBasicsCommand(
    int Step,
    Gender? Gender,
    DateOnly? DateOfBirth,
    decimal? HeightCm,
    decimal? WeightKg,
    UnitSystem? UnitPreference,
    WeightHistoryAnswer? WeighedMoreThanBefore,
    WeightTrendAnswer? RecentWeightTrend,
    BodyFatBand? BodyFat,
    ExerciseSessionsBand? ExerciseSessions,
    DailyActivityBand? DailyActivity,
    ExperienceLevel? LiftingExperience,
    ExperienceLevel? CardioExperience,
    string? ReferralSource,
    string? AffiliateCode
) : IRequest<Unit>;

public sealed class SaveOnboardingBasicsCommandValidator : AbstractValidator<SaveOnboardingBasicsCommand>
{
    public SaveOnboardingBasicsCommandValidator()
    {
        RuleFor(x => x.Step).InclusiveBetween(0, 40);
        RuleFor(x => x.Gender).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue).When(x => x.Gender.HasValue);
        RuleFor(x => x.UnitPreference).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue).When(x => x.UnitPreference.HasValue);

        // Giống MacroFactor: phải đủ 18 tuổi để tiếp tục
        RuleFor(x => x.DateOfBirth)
            .Must(date => date!.Value <= DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-18))
            .WithMessage("Bạn cần đủ 18 tuổi để tiếp tục.")
            .Must(date => date!.Value >= DateOnly.FromDateTime(DateTime.UtcNow).AddYears(-120))
            .WithMessage("Ngày sinh không hợp lệ.")
            .When(x => x.DateOfBirth.HasValue);

        RuleFor(x => x.HeightCm).InclusiveBetween(100m, 250m).WithMessage(ValidationMessages.Range).When(x => x.HeightCm.HasValue);
        RuleFor(x => x.WeightKg).InclusiveBetween(20m, 400m).WithMessage(ValidationMessages.Range).When(x => x.WeightKg.HasValue);
        RuleFor(x => x.ReferralSource).MaximumLength(50).WithMessage(ValidationMessages.MaxLength);
        RuleFor(x => x.AffiliateCode).MaximumLength(50).WithMessage(ValidationMessages.MaxLength);
    }
}

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

        await _uow.EnsureSaveAsync(ct);

        if (request.WeightKg is { } weight)
            await _sender.Send(new UpsertWeightEntryCommand(null, weight, null), ct);

        return Unit.Value;
    }
}
