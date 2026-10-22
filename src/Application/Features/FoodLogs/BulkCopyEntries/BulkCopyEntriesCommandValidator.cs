namespace Application;

public sealed class BulkCopyEntriesCommandValidator : AbstractValidator<BulkCopyEntriesCommand>
{
    public BulkCopyEntriesCommandValidator()
    {
        BulkEntrySupport.Ids(this, x => x.Ids);
        RuleFor(x => x).Must(x => x.TargetDates().Count > 0).WithName("ToDates").WithMessage(ValidationMessages.NeedToDateOrToDates);
        RuleFor(x => x).Must(x => x.TargetDates().Count <= BulkCopyEntriesCommand.MaxDates).WithName("ToDates").WithMessage(ValidationMessages.MaxDatesPerCall(BulkCopyEntriesCommand.MaxDates));
        RuleFor(x => x).Must(x => (long)(x.Ids?.Distinct().Count() ?? 0) * x.TargetDates().Count <= BulkCopyEntriesCommand.MaxCopies)
            .WithName("Ids").WithMessage(ValidationMessages.MaxCopiesPerCall(BulkCopyEntriesCommand.MaxCopies));
        RuleFor(x => x.ToMealType).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue).When(x => x.ToMealType.HasValue);
    }
}
