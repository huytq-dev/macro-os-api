namespace Application;

public sealed class BulkCopyEntriesCommandValidator : AbstractValidator<BulkCopyEntriesCommand>
{
    public BulkCopyEntriesCommandValidator()
    {
        BulkEntrySupport.Ids(this, x => x.Ids);
        RuleFor(x => x).Must(x => x.TargetDates().Count > 0).WithName("ToDates").WithMessage("Cần ToDate hoặc ToDates.");
        RuleFor(x => x).Must(x => x.TargetDates().Count <= BulkCopyEntriesCommand.MaxDates).WithName("ToDates").WithMessage($"Tối đa {BulkCopyEntriesCommand.MaxDates} ngày mỗi lần.");
        RuleFor(x => x).Must(x => (long)(x.Ids?.Distinct().Count() ?? 0) * x.TargetDates().Count <= BulkCopyEntriesCommand.MaxCopies)
            .WithName("Ids").WithMessage($"Tối đa {BulkCopyEntriesCommand.MaxCopies} entry được tạo mỗi lần (số món × số ngày).");
        RuleFor(x => x.ToMealType).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue).When(x => x.ToMealType.HasValue);
    }
}
