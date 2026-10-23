namespace Application;

public sealed class UpsertMeasurementCommandHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<UpsertMeasurementCommand, MeasurementEntryModel>
{
    public async Task<MeasurementEntryModel> Handle(UpsertMeasurementCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var m = BodyMeasurements.Find(request.Key)!;
        var locale = await UserLocale.GetAsync(_uow, userId, _timeProvider, ct);
        var date = request.Date ?? locale.Today;
        locale.EnsureNotFuture(date, "Date");

        // Nhập lại cùng số đo cùng ngày: cập nhật cột của hàng Tape hiện tại (số đo khác không đổi). Hai request đồng thời tạo hàng đầu ⇒ bên thua chạy lại như cập nhật (IdempotentWrite)
        await IdempotentWrite.RunAsync(async () =>
        {
            await FullBodyWriter.ApplyAsync(_uow, userId, date, new Dictionary<string, decimal?> { [m.Key] = request.ValueCm }, ct);
            await _uow.EnsureSaveAsync(ct);
        }, _uow.DiscardChanges);
        return new MeasurementEntryModel(date, request.ValueCm);
    }
}
