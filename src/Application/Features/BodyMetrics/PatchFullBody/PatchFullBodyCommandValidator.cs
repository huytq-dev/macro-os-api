namespace Application;

public sealed class PatchFullBodyCommandValidator : AbstractValidator<PatchFullBodyCommand>
{
    public PatchFullBodyCommandValidator()
    {
        RuleFor(x => x.Date)
            .NotInFuture()
            .When(x => x.Date.HasValue);
        RuleFor(x => x.VisualBodyFatPercent)
            .InclusiveBetween(BodyMeasurements.VisualBodyFatMin, BodyMeasurements.VisualBodyFatMax).WithMessage(ValidationMessages.NumberRange)
            .When(x => x.VisualBodyFatPercent.HasValue);
        foreach (var m in BodyMeasurements.All)
            RuleFor(x => x.Tape).Must(t => m.FromModel(t!) is null || (m.FromModel(t!) >= m.Min && m.FromModel(t!) <= m.Max))
                .WithMessage(ValidationMessages.TapeMeasurementRange(m.ModelName, m.Min, m.Max))
                .When(x => x.Tape is not null);
        RuleFor(x => x.Clear).Must(c => c is null || c.All(BodyMeasurements.IsKnownKey))
            .WithMessage(ValidationMessages.UnknownMeasurementKey);
        RuleFor(x => x).Must(x => !(x.Clear ?? []).Any(k => x.Changes().TryGetValue(BodyMeasurements.Find(k)?.Key ?? k, out var v) && v is not null))
            .WithName("Clear").WithMessage(ValidationMessages.MeasurementSetAndClear);
        RuleFor(x => x).Must(x => x.Changes().Count > 0).WithName("Tape").WithMessage(ValidationMessages.NothingToChange);
    }
}
