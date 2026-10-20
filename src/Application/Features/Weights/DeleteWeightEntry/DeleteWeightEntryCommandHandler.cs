namespace Application;

public sealed class DeleteWeightEntryCommandHandler(
    IUnitOfWork _uow,
    IUserContext _userContext,
    IOptions<AlgorithmOptions> _options,
    TimeProvider _timeProvider
) : IRequestHandler<DeleteWeightEntryCommand, Unit>
{
    public async Task<Unit> Handle(DeleteWeightEntryCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        // MỘT lần đọc: mọi dòng cân chưa xoá của user (tracked); xoá mềm dòng của ngày đó rồi tính lại trend của phần còn lại, lưu MỘT lần
        var all = await _uow.Repository<WeightEntry>().Query(tracking: true).Where(w => w.UserId == userId).ToListAsync(ct);
        var entry = all.FirstOrDefault(w => w.LogDate == request.LogDate)
            ?? throw new NotFoundException(ExceptionMessages.NotFoundField("WeightEntry", "LogDate", request.LogDate));

        await _uow.Repository<WeightEntry>().RemoveAsync(entry, ct); // interceptor chuyển thành soft delete
        WeightTrendRefresher.Apply(all.Where(w => w != entry).ToList(), _options.Value.TrendDays);
        await _uow.SaveChangesAsync(ct);
        await WeightTrendRefresher.RebuildExpenditureAsync(_uow, userId, request.LogDate, _options.Value, _timeProvider, ct);

        return Unit.Value;
    }
}
