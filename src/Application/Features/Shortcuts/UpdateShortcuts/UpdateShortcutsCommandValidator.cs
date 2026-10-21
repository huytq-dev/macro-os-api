namespace Application;

public sealed class UpdateShortcutsCommandValidator : AbstractValidator<UpdateShortcutsCommand>
{
    public UpdateShortcutsCommandValidator()
    {
        RuleFor(x => x.ToolbarStyle).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);
        RuleFor(x => x.Items).NotNull().WithMessage(ValidationMessages.Required);

        When(x => x.Items is not null, () =>
        {
            RuleFor(x => x.Items).Must(items => items.All(i => i is not null && ShortcutCatalog.Keys.Contains(i.Key)))
                .WithMessage(ValidationMessages.InvalidShortcutKey);
            RuleFor(x => x.Items).Must(items => items.Where(i => i is not null).Select(i => i.Key).Distinct().Count() == items.Count)
                .WithMessage(ValidationMessages.ShortcutDuplicated);
            RuleFor(x => x.Items).Must(items => items.Where(i => i is not null).All(i => string.IsNullOrWhiteSpace(i.Color) || ShortcutCatalog.Colors.Contains(i.Color)))
                .WithMessage(ValidationMessages.InvalidShortcutColor);
            RuleFor(x => x).Must(x => x.Items.Count(i => i is { Enabled: true }) >= ShortcutCatalog.RequiredEnabled(x.ToolbarStyle))
                .WithMessage(x => ValidationMessages.ShortcutMinEnabled(ShortcutCatalog.RequiredEnabled(x.ToolbarStyle)))
                .When(x => Enum.IsDefined(x.ToolbarStyle));
        });
    }
}
