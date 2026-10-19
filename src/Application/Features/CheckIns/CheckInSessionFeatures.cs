namespace Application;

/// <summary>Hub check-in kiểu MacroFactor: module + 7 ngày kcal (kèm ngày nghi chưa log đủ) + preview, một request.</summary>
public sealed record GetCheckInSessionQuery : IRequest<CheckInSessionResponse>;

public sealed class GetCheckInSessionQueryHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider, IOptions<AlgorithmOptions> _options)
    : IRequestHandler<GetCheckInSessionQuery, CheckInSessionResponse>
{
    public async Task<CheckInSessionResponse> Handle(GetCheckInSessionQuery request, CancellationToken ct)
        => await CheckInSupport.BuildSessionAsync(_uow, _userContext.UserId, FoodLogSupport.Today(_timeProvider), _options.Value, ct);
}

/// <summary>
/// Đặt tập ngày "log chưa đủ" trong 7 ngày gần nhất (có trong <see cref="Dates"/> = đánh dấu, không có = bỏ đánh dấu) → idempotent.
/// Expenditure được dựng lại đồng bộ (ngày đánh dấu bị bỏ qua) rồi trả session mới. Ngày chưa có log bị bỏ qua.
/// </summary>
public sealed record SetIncompleteDaysCommand(List<DateOnly> Dates) : IRequest<CheckInSessionResponse>;

public sealed class SetIncompleteDaysCommandValidator : AbstractValidator<SetIncompleteDaysCommand>
{
    public SetIncompleteDaysCommandValidator()
        => RuleFor(x => x.Dates).NotNull().WithMessage(ValidationMessages.Required)
            .Must(d => d is null || d.Count <= 7).WithMessage(ValidationMessages.MaxLength);
}

public sealed class SetIncompleteDaysCommandHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider, IOptions<AlgorithmOptions> _options)
    : IRequestHandler<SetIncompleteDaysCommand, CheckInSessionResponse>
{
    public async Task<CheckInSessionResponse> Handle(SetIncompleteDaysCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var today = FoodLogSupport.Today(_timeProvider);
        var from = today.AddDays(-6);

        var wanted = request.Dates.ToHashSet();
        if (wanted.Any(d => d < from || d > today))
            throw new BadRequestException(ExceptionMessages.IncompleteDaysOutOfRange);

        var logs = await _uow.Repository<FoodLog>().Query(tracking: true)
            .Include(l => l.Entries)
            .Where(l => l.UserId == userId && l.LogDate >= from && l.LogDate <= today)
            .ToListAsync(ct);

        DateOnly? earliest = null;
        foreach (var log in logs)
        {
            var flag = wanted.Contains(log.LogDate) && log.Entries.Count > 0; // ngày chưa log (không có entry) không đánh dấu được
            if (log.IsIncomplete == flag) continue;
            log.IsIncomplete = flag;
            if (earliest is null || log.LogDate < earliest) earliest = log.LogDate;
        }

        if (earliest is { } changedFrom)
        {
            await _uow.EnsureSaveAsync(ct);
            await ExpenditureRebuilder.RebuildAsync(_uow, userId, changedFrom, today, _options.Value, ct);
            await _uow.SaveChangesAsync(ct);
        }

        return await CheckInSupport.BuildSessionAsync(_uow, userId, today, _options.Value, ct);
    }
}

/// <summary>Cờ module check-in của user: đã xem Introduction; tắt/bật lại Partial Logging (More → Coaching Modules). Trường null = giữ nguyên.</summary>
public sealed record SetCheckInModulesCommand(bool? IntroSeen, bool? PartialLoggingHidden) : IRequest<Unit>;

public sealed class SetCheckInModulesCommandHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<SetCheckInModulesCommand, Unit>
{
    public async Task<Unit> Handle(SetCheckInModulesCommand request, CancellationToken ct)
    {
        var profile = await _uow.Repository<UserProfile>().FindTrackedAsync(p => p.UserId == _userContext.UserId, ct)
            ?? throw new NotFoundException(ExceptionMessages.NotFound("UserProfile", _userContext.UserId));

        if (request.IntroSeen is { } intro) profile.CheckInIntroSeen = intro;
        if (request.PartialLoggingHidden is { } hidden) profile.PartialLoggingModuleHidden = hidden;
        await _uow.EnsureSaveAsync(ct);
        return Unit.Value;
    }
}
