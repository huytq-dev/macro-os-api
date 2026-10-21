namespace Application;

public sealed class ResetShortcutsCommandHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<ResetShortcutsCommand, ShortcutSettingsModel>
{
    public async Task<ShortcutSettingsModel> Handle(ResetShortcutsCommand request, CancellationToken ct)
    {
        var row = await _uow.Repository<UserShortcutSetting>().FindTrackedAsync(s => s.UserId == _userContext.UserId, ct);
        if (row is not null)
        {
            await _uow.Repository<UserShortcutSetting>().RemoveAsync(row, ct);
            await _uow.EnsureSaveAsync(ct);
        }
        return GetShortcutsQueryHandler.Defaults();
    }
}
