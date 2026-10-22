namespace Application;

public sealed class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.FirstName)
            .MaximumLength(100).WithMessage(ValidationMessages.MaxLength)
            .When(x => x.FirstName is not null);

        RuleFor(x => x.LastName)
            .MaximumLength(100).WithMessage(ValidationMessages.MaxLength)
            .When(x => x.LastName is not null);

        RuleFor(x => x.HeightCm)
            .InclusiveBetween(50, 300).WithMessage(ValidationMessages.NumberRange)
            .When(x => x.HeightCm.HasValue);

        RuleFor(x => x.Bio)
            .MaximumLength(500).WithMessage(ValidationMessages.MaxLength)
            .When(x => x.Bio is not null);

        RuleFor(x => x.TimeZone)
            .MaximumLength(100).WithMessage(ValidationMessages.MaxLength)
            .When(x => x.TimeZone is not null);

        RuleFor(x => x.Locale)
            .MaximumLength(20).WithMessage(ValidationMessages.MaxLength)
            .When(x => x.Locale is not null);

        RuleFor(x => x.Gender)
            .IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue)
            .When(x => x.Gender.HasValue);

        RuleFor(x => x.ActivityLevel)
            .IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue)
            .When(x => x.ActivityLevel.HasValue);

        RuleFor(x => x.UnitPreference)
            .IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue)
            .When(x => x.UnitPreference.HasValue);

        RuleFor(x => x.EnergyUnit)
            .IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue)
            .When(x => x.EnergyUnit.HasValue);

        RuleFor(x => x.WeekStartDay)
            .IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue)
            .When(x => x.WeekStartDay.HasValue);
    }
}
