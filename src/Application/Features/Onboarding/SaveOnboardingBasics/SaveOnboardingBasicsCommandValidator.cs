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
