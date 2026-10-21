namespace Application;

public sealed class UpsertMeasurementCommandValidator : AbstractValidator<UpsertMeasurementCommand>
{
    public UpsertMeasurementCommandValidator()
    {
        RuleFor(x => x.Key).Must(k => BodyMeasurements.Find(k) is not null).WithMessage(ValidationMessages.UnknownMeasurementKey);
        RuleFor(x => x).Must(x => BodyMeasurements.Find(x.Key) is not { } m || (x.ValueCm >= m.Min && x.ValueCm <= m.Max))
            .WithName("ValueCm")
            .WithMessage(x => BodyMeasurements.Find(x.Key) is { } m ? ValidationMessages.TapeMeasurementRange(m.ModelName, m.Min, m.Max) : ValidationMessages.UnknownMeasurementKey);
        RuleFor(x => x.Date)
            .LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)))
            .WithMessage(ValidationMessages.NotInFuture)
            .When(x => x.Date.HasValue);
    }
}
