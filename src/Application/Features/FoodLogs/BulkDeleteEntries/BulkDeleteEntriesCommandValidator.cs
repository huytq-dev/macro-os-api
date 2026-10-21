namespace Application;

public sealed class BulkDeleteEntriesCommandValidator : AbstractValidator<BulkDeleteEntriesCommand>
{
    public BulkDeleteEntriesCommandValidator() => BulkEntrySupport.Ids(this, x => x.Ids);
}
