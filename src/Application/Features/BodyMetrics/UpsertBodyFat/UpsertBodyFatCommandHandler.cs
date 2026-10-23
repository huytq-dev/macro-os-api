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

        // Một giá trị / (user, ngày, phương pháp): nhập lại cùng ngày CẬP NHẬT hàng hiện tại, không tạo điểm mới. Dữ liệu cũ có thể có nhiều hàng trùng: ghi vào hàng MỚI NHẤT, cùng quy tắc ĐỌC.
        // Hai request đồng thời tạo dòng đầu: unique index (nếu đã áp dụng) làm một bên thua ⇒ chạy lại một lần như cập nhật dòng của bên thắng (IdempotentWrite)
        return await IdempotentWrite.RunAsync(async () =>
        {
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
        }, _uow.DiscardChanges);
    }
}
