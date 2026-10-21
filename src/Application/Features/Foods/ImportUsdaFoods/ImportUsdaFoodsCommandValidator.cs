namespace Application;

public sealed class ImportUsdaFoodsCommandValidator : AbstractValidator<ImportUsdaFoodsCommand>
{
    public ImportUsdaFoodsCommandValidator()
    {
        RuleFor(x => x).Must(x => !string.IsNullOrWhiteSpace(x.Query) || x.FdcIds is { Count: > 0 })
            .WithName("Query").WithMessage("Cần Query hoặc FdcIds.");
        RuleFor(x => x.Query).MaximumLength(200).WithMessage(ValidationMessages.MaxLength);
        RuleFor(x => x.MaxItems).InclusiveBetween(1, 50);
        RuleFor(x => x.FdcIds).Must(l => l is null || (l.Count <= 50 && l.All(i => i > 0))).WithMessage("Tối đa 50 fdcId dương.");
    }
}
