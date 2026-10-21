namespace Application;

public sealed class AnalyzeMealCommandValidator : AbstractValidator<AnalyzeMealCommand>
{
    public AnalyzeMealCommandValidator(IOptions<AiOptions> options)
    {
        var o = options.Value;
        var maxMb = o.MaxPhotoBytes / (1024 * 1024);

        RuleFor(x => x.Photos)
            .Must((cmd, photos) => photos.Count > 0 || !string.IsNullOrWhiteSpace(cmd.Description))
            .WithMessage(ValidationMessages.AiMealNeedsInput)
            .Must(photos => photos.Count <= o.MaxPhotos)
            .WithMessage(ValidationMessages.AiMealMaxPhotos(o.MaxPhotos));
        RuleForEach(x => x.Photos)
            .Must(p => p.Data.Length > 0 && p.Data.Length <= o.MaxPhotoBytes)
            .WithMessage(ValidationMessages.AiMealPhotoMaxMb(maxMb))
            .Must(p => p.MediaType is not null)
            .WithMessage(ValidationMessages.AiMealPhotoType);
        RuleFor(x => x.Description).MaximumLength(o.MaxDescriptionLength).WithMessage(ValidationMessages.MaxLength);
    }
}
