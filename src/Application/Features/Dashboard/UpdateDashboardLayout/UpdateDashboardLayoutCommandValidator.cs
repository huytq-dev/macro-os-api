namespace Application;

public sealed class UpdateDashboardLayoutCommandValidator : AbstractValidator<UpdateDashboardLayoutCommand>
{
    public UpdateDashboardLayoutCommandValidator()
    {
        RuleFor(x => x.Widgets).NotNull().WithMessage(ValidationMessages.Required);
        RuleForEach(x => x.Widgets).ChildRules(w =>
        {
            w.RuleFor(i => i.WidgetKey).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);
            w.RuleFor(i => i.SortOrder).GreaterThanOrEqualTo(0);
        });
        RuleFor(x => x.Widgets)
            .Must(ws => ws.Select(i => i.WidgetKey).Distinct().Count() == ws.Count)
            .WithMessage(ValidationMessages.DuplicateWidgetKey)
            .When(x => x.Widgets is not null);
    }
}
