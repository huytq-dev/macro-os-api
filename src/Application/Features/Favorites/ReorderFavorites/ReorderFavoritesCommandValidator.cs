namespace Application;

public sealed class ReorderFavoritesCommandValidator : AbstractValidator<ReorderFavoritesCommand>
{
    public ReorderFavoritesCommandValidator()
    {
        RuleFor(x => x.Ids).NotNull().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.Ids).Must(ids => ids is null || ids.Count <= 500 && ids.Distinct().Count() == ids.Count)
            .WithMessage("Danh sách không hợp lệ.");
    }
}
