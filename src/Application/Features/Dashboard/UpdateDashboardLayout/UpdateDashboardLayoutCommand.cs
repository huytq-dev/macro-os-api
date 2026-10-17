namespace Application;

public sealed record DashboardWidgetItem(DashboardWidget WidgetKey, int SortOrder, bool IsVisible);

/// <summary>Thay toàn bộ layout dashboard (idempotent). Widget vắng mặt trong danh sách quay về mặc định; danh sách rỗng = reset hết.</summary>
public sealed record UpdateDashboardLayoutCommand(IReadOnlyList<DashboardWidgetItem> Widgets) : IRequest<Unit>;

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
            .WithMessage("WidgetKey bị trùng.")
            .When(x => x.Widgets is not null);
    }
}

public sealed class UpdateDashboardLayoutCommandHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<UpdateDashboardLayoutCommand, Unit>
{
    public async Task<Unit> Handle(UpdateDashboardLayoutCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var repo = _uow.Repository<DashboardWidgetSetting>();
        var rows = (await repo.GetAllTrackedAsync(s => s.UserId == userId, ct)).ToDictionary(s => s.WidgetKey);

        foreach (var item in request.Widgets)
        {
            if (rows.Remove(item.WidgetKey, out var row))
            {
                row.SortOrder = item.SortOrder;
                row.IsVisible = item.IsVisible;
            }
            else
            {
                await repo.AddAsync(new DashboardWidgetSetting
                {
                    Id = Guid.NewGuid(), UserId = userId, WidgetKey = item.WidgetKey,
                    SortOrder = item.SortOrder, IsVisible = item.IsVisible,
                }, ct);
            }
        }
        if (rows.Count > 0) await repo.RemoveRangeAsync(rows.Values, ct);

        await _uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
