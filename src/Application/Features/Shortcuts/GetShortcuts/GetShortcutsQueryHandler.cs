namespace Application;

public sealed class GetShortcutsQueryHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<GetShortcutsQuery, ShortcutSettingsModel>
{
    public async Task<ShortcutSettingsModel> Handle(GetShortcutsQuery request, CancellationToken ct)
    {
        var row = await _uow.Repository<UserShortcutSetting>().FindAsync(s => s.UserId == _userContext.UserId, ct);
        return row is null ? Defaults() : ToModel(row);
    }

    internal static ShortcutSettingsModel Defaults() => new(ToolbarStyle.SearchAndBarcode.ToString(), ShortcutCatalog.Defaults(), true);

    internal static ShortcutSettingsModel ToModel(UserShortcutSetting row) => new(row.ToolbarStyle.ToString(), ShortcutCatalog.Deserialize(row.ShortcutsJson), false);
}
