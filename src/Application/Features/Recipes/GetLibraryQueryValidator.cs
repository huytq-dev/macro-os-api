namespace Application;

public sealed class GetLibraryQueryValidator : AbstractValidator<GetLibraryQuery>
{
    public GetLibraryQueryValidator() => RuleFor(x => x.Scope).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);
}
