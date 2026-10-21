namespace Application;

public sealed class UpdateShortcutsCommandHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<UpdateShortcutsCommand, ShortcutSettingsModel>
{
    public async Task<ShortcutSettingsModel> Handle(UpdateShortcutsCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var row = await _uow.Repository<UserShortcutSetting>().FindTrackedAsync(s => s.UserId == userId, ct);
        if (row is null)
        {
            row = new UserShortcutSetting { Id = Guid.NewGuid(), UserId = userId };
            await _uow.Repository<UserShortcutSetting>().AddAsync(row, ct);
        }

        row.ToolbarStyle = request.ToolbarStyle;
        row.ShortcutsJson = ShortcutCatalog.Serialize(ShortcutCatalog.Normalize(request.Items));
        await _uow.EnsureSaveAsync(ct);
        return GetShortcutsQueryHandler.ToModel(row);
    }
}
