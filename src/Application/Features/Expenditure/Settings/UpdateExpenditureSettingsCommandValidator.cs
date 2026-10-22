namespace Application;

public sealed class UpdateExpenditureSettingsCommandValidator : AbstractValidator<UpdateExpenditureSettingsCommand>
{
    public UpdateExpenditureSettingsCommandValidator()
    {
        RuleFor(x => x.InitialEstimateMode).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue).When(x => x.InitialEstimateMode.HasValue);
        RuleFor(x => x.BmrEquation).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue).When(x => x.BmrEquation.HasValue);
        RuleFor(x => x.BmrSex).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue).When(x => x.BmrSex.HasValue);
        RuleFor(x => x.WeighedMoreThanBefore).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue).When(x => x.WeighedMoreThanBefore.HasValue);
        RuleFor(x => x.PriorWeightThresholdShown).InclusiveBetween(1m, 2000m).WithMessage(ValidationMessages.NumberRange).When(x => x.PriorWeightThresholdShown.HasValue);
        RuleFor(x => x.PriorWeightThresholdUnit).Must(u => u is "kg" or "lb").WithMessage(ValidationMessages.InvalidFormat).When(x => x.PriorWeightThresholdUnit is not null);
        RuleFor(x => x).Must(x => (x.PriorWeightThresholdShown is null) == (x.PriorWeightThresholdUnit is null)).WithName("PriorWeightThresholdShown").WithMessage(ValidationMessages.Required);
        RuleFor(x => x.AthleticPursuits).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue).When(x => x.AthleticPursuits.HasValue);

        RuleFor(x => x.ManualInitialTdeeKcal).InclusiveBetween(1000, 6000).WithMessage(ValidationMessages.NumberRange).When(x => x.ManualInitialTdeeKcal.HasValue);
        // Nhập tay nhưng chọn Formula là mâu thuẫn; chọn Manual mà không có giá trị (và chưa có sẵn) được handler từ chối
        RuleFor(x => x.ManualInitialTdeeKcal).Null().WithMessage(ValidationMessages.InvalidFormat)
            .When(x => x.InitialEstimateMode == Domain.InitialEstimateMode.Formula);
    }
}
