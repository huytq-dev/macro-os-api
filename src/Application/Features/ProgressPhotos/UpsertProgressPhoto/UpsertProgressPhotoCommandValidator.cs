namespace Application;

public sealed class UpsertProgressPhotoCommandValidator : AbstractValidator<UpsertProgressPhotoCommand>
{
    public UpsertProgressPhotoCommandValidator(IOptions<ProgressPhotoOptions> options, TimeProvider timeProvider)
    {
        RuleFor(x => x.Angle).Must(a => a is PhotoAngle.Front or PhotoAngle.Side or PhotoAngle.Back)
            .WithMessage(ValidationMessages.InvalidPhotoAngle);
        RuleFor(x => x.Data).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.Data).Must(d => d.Length <= options.Value.MaxBytes)
            .WithMessage(ValidationMessages.PhotoMaxMb(options.Value.MaxBytes / (1024 * 1024)))
            .When(x => x.Data is { Length: > 0 });
        RuleFor(x => x.Date)
            .LessThanOrEqualTo(_ => DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime.AddDays(1)))
            .WithMessage(ValidationMessages.NotInFuture)
            .When(x => x.Date.HasValue);
    }
}
