namespace Application;

public sealed class UpsertFullBodyCommandValidator : AbstractValidator<UpsertFullBodyCommand>
{
    public UpsertFullBodyCommandValidator(TimeProvider timeProvider)
    {
        RuleFor(x => x.Date)
            .LessThanOrEqualTo(_ => DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime.AddDays(1)))
            .WithMessage(ValidationMessages.NotInFuture)
            .When(x => x.Date.HasValue);
        RuleFor(x => x.VisualBodyFatPercent)
            .InclusiveBetween(BodyMeasurements.VisualBodyFatMin, BodyMeasurements.VisualBodyFatMax).WithMessage(ValidationMessages.NumberRange)
            .When(x => x.VisualBodyFatPercent.HasValue);

        // Mỗi ô số đo (nếu nhập) trong khoảng hợp lý của chính số đo đó (BodyMeasurements.All)
        foreach (var m in BodyMeasurements.All)
            RuleFor(x => x.Tape).Must(t => m.FromModel(t!) is null || (m.FromModel(t!) >= m.Min && m.FromModel(t!) <= m.Max))
                .WithMessage(ValidationMessages.TapeMeasurementRange(m.ModelName, m.Min, m.Max))
                .When(x => x.Tape is not null);
    }
}
