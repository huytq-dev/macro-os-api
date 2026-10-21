namespace Application;

public sealed class DeleteProgressPhotosCommandHandler(
    IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider, IPrivateImageStore _store, ILogger<DeleteProgressPhotosCommandHandler> _logger)
    : IRequestHandler<DeleteProgressPhotosCommand, Unit>
{
    public async Task<Unit> Handle(DeleteProgressPhotosCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var date = request.Date ?? (await UserLocale.GetAsync(_uow, userId, _timeProvider, ct)).Today;
        var angle = request.Angle;
        var rows = await _uow.Repository<ProgressPhoto>().GetAllTrackedAsync(p => p.UserId == userId && p.PhotoDate == date && (angle == null || p.Angle == angle), ct);
        if (rows.Count == 0) return Unit.Value;

        var keys = rows.Select(r => r.StorageKey).ToList();
        await _uow.Repository<ProgressPhoto>().RemoveRangeAsync(rows, ct); // interceptor chuyển thành soft delete
        await _uow.EnsureSaveAsync(ct);

        foreach (var key in keys)
        {
            try { await _store.DeleteAsync(key, CancellationToken.None); }
            catch (ExternalServiceException ex) { _logger.LogWarning(ex, "Không xoá được ảnh tiến độ khỏi kho"); }
        }
        return Unit.Value;
    }
}
