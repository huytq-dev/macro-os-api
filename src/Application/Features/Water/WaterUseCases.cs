namespace Application;

/// <summary>WP2: ghi một lần uống nước (LogDate null = hôm nay; Time thiếu → giờ hiện tại nếu là hôm nay, ngược lại 12:00). Cập nhật FoodLog.TotalWater.</summary>
public sealed record AddWaterCommand(DateOnly? LogDate, decimal AmountMl, TimeOnly? Time, string? Source) : IRequest<Guid>;

public sealed class AddWaterCommandValidator : AbstractValidator<AddWaterCommand>
{
    public AddWaterCommandValidator()
    {
        RuleFor(x => x.AmountMl).InclusiveBetween(1m, 5000m);
        RuleFor(x => x.Source).MaximumLength(50).WithMessage(ValidationMessages.MaxLength);
        RuleFor(x => x.LogDate)
            .LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)))
            .WithMessage("{PropertyName} không được ở tương lai.")
            .When(x => x.LogDate.HasValue);
    }
}

public sealed class AddWaterCommandHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<AddWaterCommand, Guid>
{
    public async Task<Guid> Handle(AddWaterCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var date = request.LogDate ?? FoodLogSupport.Today(_timeProvider);
        var log = await FoodLogSupport.GetOrCreateAsync(_uow, userId, date, ct);
        var (time, _) = await FoodLogSupport.ResolveTimeAsync(_uow, userId, date, request.Time, null, _timeProvider, ct);

        var water = new WaterLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FoodLogId = log.Id,
            AmountMl = request.AmountMl,
            LoggedAt = new DateTimeOffset(date.ToDateTime(time), TimeSpan.Zero),
            Source = string.IsNullOrWhiteSpace(request.Source) ? null : request.Source.Trim(),
        };
        await _uow.Repository<WaterLog>().AddAsync(water, ct);
        log.TotalWater = await _uow.Repository<WaterLog>().Query().Where(w => w.FoodLogId == log.Id).SumAsync(w => w.AmountMl, ct) + request.AmountMl;

        await WaterSupport.SaveAsync(_uow, userId, date, _timeProvider, ct);
        return water.Id;
    }
}

/// <summary>WP2: các lần uống nước của một ngày (mặc định hôm nay), theo giờ.</summary>
public sealed record GetDayWaterQuery(DateOnly? Date) : IRequest<DayWaterResponse>;

public sealed class GetDayWaterQueryHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<GetDayWaterQuery, DayWaterResponse>
{
    public async Task<DayWaterResponse> Handle(GetDayWaterQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var date = request.Date ?? FoodLogSupport.Today(_timeProvider);
        var rows = await _uow.Repository<WaterLog>().Query()
            .Where(w => w.UserId == userId && w.FoodLog.LogDate == date)
            .OrderBy(w => w.LoggedAt).ThenBy(w => w.Id)
            .Select(w => new { w.Id, w.AmountMl, w.LoggedAt, w.Source })
            .ToListAsync(ct);

        var logs = rows.Select(w => new WaterLogModel(w.Id, w.AmountMl, TimeOnly.FromTimeSpan(w.LoggedAt.TimeOfDay), w.Source)).ToList();
        return new DayWaterResponse(date, logs.Sum(l => l.AmountMl), logs);
    }
}

/// <summary>WP2: xoá một lần uống nước; tính lại FoodLog.TotalWater.</summary>
public sealed record DeleteWaterCommand(Guid Id) : IRequest<Unit>;

public sealed class DeleteWaterCommandHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<DeleteWaterCommand, Unit>
{
    public async Task<Unit> Handle(DeleteWaterCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var water = await _uow.Repository<WaterLog>().Query(tracking: true)
            .Include(w => w.FoodLog)
            .FirstOrDefaultAsync(w => w.Id == request.Id && w.UserId == userId, ct)
            ?? throw new NotFoundException(ExceptionMessages.NotFound("WaterLog", request.Id));

        var log = water.FoodLog;
        log.TotalWater = await _uow.Repository<WaterLog>().Query().Where(w => w.FoodLogId == log.Id && w.Id != water.Id).SumAsync(w => w.AmountMl, ct);
        await _uow.Repository<WaterLog>().RemoveAsync(water, ct);

        await WaterSupport.SaveAsync(_uow, userId, log.LogDate, _timeProvider, ct);
        return Unit.Value;
    }
}

internal static class WaterSupport
{
    // ponytail: nước chỉ đổi water_g của ngày nên chỉ ghi lại aggregate, không dựng lại expenditure (thuật toán không dùng nước); DailyProgressSnapshot.WaterConsumed cập nhật ở lần rebuild kế tiếp
    public static async Task SaveAsync(IUnitOfWork uow, Guid userId, DateOnly date, TimeProvider tp, CancellationToken ct)
    {
        await uow.EnsureSaveAsync(ct);
        await NutritionAggregates.RefreshAsync(uow, userId, [date], tp, ct);
        await uow.SaveChangesAsync(ct);
    }
}
