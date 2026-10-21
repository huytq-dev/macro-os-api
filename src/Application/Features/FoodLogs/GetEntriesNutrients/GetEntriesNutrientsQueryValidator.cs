namespace Application;

public sealed class GetEntriesNutrientsQueryValidator : AbstractValidator<GetEntriesNutrientsQuery>
{
    public GetEntriesNutrientsQueryValidator() => BulkEntrySupport.Ids(this, x => x.Ids);
}
