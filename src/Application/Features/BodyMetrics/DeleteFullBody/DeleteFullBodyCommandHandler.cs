namespace Application;

public sealed class DeleteFullBodyCommandHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<DeleteFullBodyCommand, Unit>
{
    public async Task<Unit> Handle(DeleteFullBodyCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var date = request.Date ?? (await UserLocale.GetAsync(_uow, userId, _timeProvider, ct)).Today;
        // Xoá ở MỌI hàng Visual/Tape của ngày (DB không chặn trùng ngày): không để giá trị của hàng trùng cũ hiện lại như điểm ma
        await FullBodyWriter.ApplyAsync(_uow, userId, date, FullBodySupport.ReplaceChanges(null, null), ct);
        await _uow.EnsureSaveAsync(ct);
        return Unit.Value;
    }
}
