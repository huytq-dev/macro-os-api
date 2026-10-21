namespace Application;

/// <summary>
/// Bật/tắt MỘT ngày kinh (sheet "Are you on your period?" của MacroFactor), idempotent: bật ngày đã theo dõi / tắt ngày chưa theo dõi = không đổi.
/// Bật → kéo dài kỳ sát ngày đó, gộp hai kỳ nếu lấp đúng khe 1 ngày, hoặc tạo kỳ [ngày, ngày]. Tắt → xoá kỳ 1 ngày, thu hẹp đầu/cuối, hoặc tách đôi khi ở giữa. Tất cả trong MỘT lần lưu (nguyên tử);
/// độ dài chu kỳ / kỳ tính lại cả chuỗi. Ngày ở tương lai (theo múi giờ user) → 400. Ngày lấy từ route (JsonIgnore, nullable để model binding không đòi trong body).
/// </summary>
public sealed record SetPeriodDayCommand(
    [property: System.Text.Json.Serialization.JsonIgnore] DateOnly? Date,
    bool? IsOnPeriod
) : IRequest<Unit>;

public sealed class SetPeriodDayCommandValidator : AbstractValidator<SetPeriodDayCommand>
{
    public SetPeriodDayCommandValidator()
        => RuleFor(x => x.IsOnPeriod).NotNull().WithMessage(ValidationMessages.Required);
}

public sealed class SetPeriodDayCommandHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<SetPeriodDayCommand, Unit>
{
    public async Task<Unit> Handle(SetPeriodDayCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var date = request.Date ?? throw new BadRequestException(ValidationMessages.Required);
        var locale = await UserLocale.GetAsync(_uow, userId, _timeProvider, ct);
        locale.EnsureNotFuture(date, "Date");

        var logs = (await _uow.Repository<PeriodLog>().GetAllTrackedAsync(p => p.UserId == userId, ct)).ToList();
        var edit = PeriodDays.SetDay(logs.Select(l => new PeriodRange(l.Id, l.StartDate, l.EndDate, l.Symptoms, l.Notes)).ToList(), date, request.IsOnPeriod == true, locale.Today);
        if (!edit.Changed) return Unit.Value;

        var repo = _uow.Repository<PeriodLog>();
        var keep = edit.Ranges.Select(r => r.Id).ToHashSet();
        foreach (var gone in logs.Where(l => !keep.Contains(l.Id)).ToList())
        {
            await repo.RemoveAsync(gone, ct);
            logs.Remove(gone);
        }
        foreach (var r in edit.Ranges)
        {
            var log = logs.FirstOrDefault(l => l.Id == r.Id);
            if (log is null)
            {
                log = new PeriodLog { Id = r.Id, UserId = userId };
                await repo.AddAsync(log, ct);
                logs.Add(log);
            }
            (log.StartDate, log.EndDate, log.Symptoms, log.Notes) = (r.Start, r.End, r.Symptoms, r.Notes);
        }
        PeriodSupport.Recompute(logs);

        await _uow.SaveChangesAsync(ct); // một SaveChanges = một transaction: mọi thay đổi của gộp/tách cùng thành công hoặc cùng huỷ
        return Unit.Value;
    }
}
