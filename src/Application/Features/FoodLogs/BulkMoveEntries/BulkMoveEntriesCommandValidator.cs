namespace Application;

public sealed class BulkMoveEntriesCommandValidator : AbstractValidator<BulkMoveEntriesCommand>
{
    public BulkMoveEntriesCommandValidator()
    {
        BulkEntrySupport.Ids(this, x => x.Ids);
        RuleFor(x => x).Must(x => x.ToDate.HasValue || x.ToTime.HasValue || x.ToMealType.HasValue)
            .WithName("ToDate").WithMessage("Cần ít nhất một trong ToDate, ToTime, ToMealType.");
        RuleFor(x => x.ToMealType).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue).When(x => x.ToMealType.HasValue);
    }
}
