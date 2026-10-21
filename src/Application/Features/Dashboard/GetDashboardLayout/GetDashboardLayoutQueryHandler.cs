namespace Application;

public sealed class GetDashboardLayoutQueryHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<GetDashboardLayoutQuery, DashboardLayoutResponse>
{
    public async Task<DashboardLayoutResponse> Handle(GetDashboardLayoutQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var saved = await _uow.Repository<DashboardWidgetSetting>().Query()
            .Where(s => s.UserId == userId)
            .OrderBy(s => s.SortOrder).ThenBy(s => s.WidgetKey)
            .Select(s => new { s.WidgetKey, s.SortOrder, s.IsVisible })
            .ToListAsync(ct);

        var widgets = saved.Select(s => new DashboardWidgetModel(s.WidgetKey.ToString(), s.SortOrder, s.IsVisible)).ToList();
        var next = widgets.Count == 0 ? 0 : widgets.Max(w => w.SortOrder) + 1;
        foreach (var key in Enum.GetValues<DashboardWidget>().Where(k => saved.All(s => s.WidgetKey != k)))
            widgets.Add(new DashboardWidgetModel(key.ToString(), next++, true));

        return new DashboardLayoutResponse(widgets);
    }
}
