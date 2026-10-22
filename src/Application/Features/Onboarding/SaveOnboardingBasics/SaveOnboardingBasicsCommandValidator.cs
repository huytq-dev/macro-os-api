namespace Application;

public sealed class SaveOnboardingBasicsCommandValidator : AbstractValidator<SaveOnboardingBasicsCommand>
{
    public SaveOnboardingBasicsCommandValidator()
    {
        RuleFor(x => x.Step).InclusiveBetween(0, 40);
        RuleFor(x => x.Gender).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue).When(x => x.Gender.HasValue);
        RuleFor(x => x.UnitPreference).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue).When(x => x.UnitPreference.HasValue);

        // Giống MacroFactor: phải đủ 18 tuổi để tiếp tục
        RuleFor(x => x.DateOfBirth)
            .Must(date => date!.Value <= DateRuleBuilderExtensions.UtcToday.AddDays(1).AddYears(-18))   // dung sai +1 ngày (múi giờ phía đông UTC); handler kiểm đúng theo ngày LOCAL
            .WithMessage(ValidationMessages.MustBeAdult)
            .Must(date => date!.Value >= DateRuleBuilderExtensions.UtcToday.AddYears(-120))
            .WithMessage(ValidationMessages.InvalidBirthDate)
            .When(x => x.DateOfBirth.HasValue);

        RuleFor(x => x.HeightCm).InclusiveBetween(100m, 250m).WithMessage(ValidationMessages.NumberRange).When(x => x.HeightCm.HasValue);
        RuleFor(x => x.WeightKg).InclusiveBetween(20m, 400m).WithMessage(ValidationMessages.NumberRange).When(x => x.WeightKg.HasValue);
        RuleFor(x => x.PriorWeightThresholdShown).InclusiveBetween(1m, 2000m).WithMessage(ValidationMessages.NumberRange).When(x => x.PriorWeightThresholdShown.HasValue);
        RuleFor(x => x.PriorWeightThresholdUnit).Must(u => u is "kg" or "lb").WithMessage(ValidationMessages.InvalidFormat).When(x => x.PriorWeightThresholdUnit is not null);
        RuleFor(x => x).Must(x => (x.PriorWeightThresholdShown is null) == (x.PriorWeightThresholdUnit is null)).WithName("PriorWeightThresholdShown").WithMessage(ValidationMessages.Required);
        RuleFor(x => x.ReferralSource).MaximumLength(50).WithMessage(ValidationMessages.MaxLength);
        RuleFor(x => x.AffiliateCode).MaximumLength(50).WithMessage(ValidationMessages.MaxLength);
    }
}
