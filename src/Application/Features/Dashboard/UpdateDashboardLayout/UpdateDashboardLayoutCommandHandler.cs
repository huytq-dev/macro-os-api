namespace Application;

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
