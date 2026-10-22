namespace Application;

public sealed class GetProgressPhotoSessionsQueryValidator : AbstractValidator<GetProgressPhotoSessionsQuery>
{
    public GetProgressPhotoSessionsQueryValidator()
        => RuleFor(x => x).Must(x => x.From is null || x.To is null || x.From <= x.To).WithName("From").WithMessage(ValidationMessages.FromAfterTo);
}
