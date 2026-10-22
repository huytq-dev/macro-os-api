namespace Application;

public sealed class SearchFoodsGroupedQueryValidator : AbstractValidator<SearchFoodsGroupedQuery>
{
    public SearchFoodsGroupedQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage(ValidationMessages.GreaterThanOrEqual);
        RuleFor(x => x.Top).InclusiveBetween(0, SearchFoodsGroupedQuery.MaxTop).WithMessage(ValidationMessages.NumberRange);
        RuleFor(x => x.Search).MaximumLength(100).WithMessage(ValidationMessages.MaxLength);
        RuleFor(x => x.Section!.Value).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue).When(x => x.Section.HasValue);
    }
}
