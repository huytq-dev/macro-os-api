namespace Application;

public sealed class UpsertBodyFatCommandHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<UpsertBodyFatCommand, Guid>
{
    public async Task<Guid> Handle(UpsertBodyFatCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var locale = await UserLocale.GetAsync(_uow, userId, _timeProvider, ct);
        var date = request.MeasuredDate ?? locale.Today;
        locale.EnsureNotFuture(date, "MeasuredDate");

        // Ngày có thể có nhiều hàng cùng phương pháp (DB không chặn trùng): ghi vào hàng MỚI NHẤT, cùng quy tắc ĐỌC (series/overview lấy hàng mới nhất)
        var row = await _uow.Repository<BodyMetric>().Query(tracking: true)
            .Where(b => b.UserId == userId && b.MeasuredDate == date && b.Method == request.Method)
            .OrderByDescending(b => b.CreatedAt)
            .FirstOrDefaultAsync(ct);
        if (row is null)
        {
            row = new BodyMetric { Id = Guid.NewGuid(), UserId = userId, MeasuredDate = date, Method = request.Method };
            await _uow.Repository<BodyMetric>().AddAsync(row, ct);
        }
        row.BodyFatPercent = request.BodyFatPercent;
        row.Notes = request.Notes;

        await _uow.SaveChangesAsync(ct);
        return row.Id;
    }
}
