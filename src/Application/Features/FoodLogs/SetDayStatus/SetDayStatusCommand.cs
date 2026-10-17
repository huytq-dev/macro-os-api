namespace Application;

/// <summary>
/// UC-D15: đặt trạng thái ngày (Complete / Partial / Fasting / Unmarked). Partial bỏ ngày khỏi thuật toán, Fasting là 0 kcal thật
/// (chỉ ngày không có món). Tạo FoodLog rỗng nếu cần (đánh dấu Fasting ngày trống).
/// </summary>
public sealed record SetDayStatusCommand(
    [property: System.Text.Json.Serialization.JsonIgnore] DateOnly Date,
    FoodLogStatus Status
) : IRequest<Unit>;

public sealed class SetDayStatusCommandValidator : AbstractValidator<SetDayStatusCommand>
{
    public SetDayStatusCommandValidator()
    {
        RuleFor(x => x.Status).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);
        RuleFor(x => x.Date)
            .LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)))
            .WithMessage("{PropertyName} không được ở tương lai.");
    }
}

public sealed class SetDayStatusCommandHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider, IOptions<AlgorithmOptions> _options)
    : IRequestHandler<SetDayStatusCommand, Unit>
{
    public async Task<Unit> Handle(SetDayStatusCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var log = await _uow.Repository<FoodLog>().Query(tracking: true)
            .Include(l => l.Entries)
            .FirstOrDefaultAsync(l => l.UserId == userId && l.LogDate == request.Date, ct);

        if (log is null)
        {
            if (request.Status == FoodLogStatus.Unmarked) return Unit.Value; // không có gì để bỏ đánh dấu
            log = new FoodLog { Id = Guid.NewGuid(), UserId = userId, LogDate = request.Date };
            await _uow.Repository<FoodLog>().AddAsync(log, ct);
        }
        else if (log.Status == request.Status)
        {
            return Unit.Value;
        }

        if (request.Status == FoodLogStatus.Fasting && log.Entries.Any(e => !e.IsDeleted))
            throw new BadRequestException("Ngày đã có món ăn, không thể đánh dấu Fasting.");

        log.Status = request.Status;
        log.StatusSource = DayStatusSource.User;

        await FoodLogSupport.SaveAndRebuildAsync(_uow, userId, request.Date, _options.Value, _timeProvider, ct);
        return Unit.Value;
    }
}
