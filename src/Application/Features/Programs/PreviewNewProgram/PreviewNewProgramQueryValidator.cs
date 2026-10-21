namespace Application;

public sealed class PreviewNewProgramQueryValidator : AbstractValidator<PreviewNewProgramQuery>
{
    public PreviewNewProgramQueryValidator() => RuleFor(x => x.Draft).SetValidator(new CreateProgramCommandValidator());
}
