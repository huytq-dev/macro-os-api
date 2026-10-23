namespace Application;

public sealed class UpsertFullBodyCommandHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<UpsertFullBodyCommand, FullBodyResponse>
{
    public async Task<FullBodyResponse> Handle(UpsertFullBodyCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var locale = await UserLocale.GetAsync(_uow, userId, _timeProvider, ct);
        var date = request.Date ?? locale.Today;
        locale.EnsureNotFuture(date, "Date");

        // Thay cả trạng thái của ngày qua MỘT đường ghi chung (FullBodyWriter): cập nhật hàng hiện tại, xoá ở mọi hàng trùng ngày, không tạo thêm điểm
        var (visual, tape) = await IdempotentWrite.RunAsync(async () =>
        {
            var (v, t, _) = await FullBodyWriter.ApplyAsync(_uow, userId, date, FullBodySupport.ReplaceChanges(request.VisualBodyFatPercent, request.Tape), ct);
            await _uow.EnsureSaveAsync(ct);
            return (v, t);
        }, _uow.DiscardChanges);
        var weights = await FullBodyWriter.WeightsAsync(_uow, userId, date, date, ct);
        return FullBodySupport.Build(date, visual, tape, weights.TryGetValue(date, out var w) ? w.Scale : null, weights.TryGetValue(date, out w) ? w.Trend : null);
    }
}
