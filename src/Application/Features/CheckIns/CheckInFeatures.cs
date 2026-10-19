namespace Application;

/// <summary>UC-G02: tuần qua + đề xuất target tuần tới + lý do. Xem được bất kỳ lúc nào; IsDue cho biết đã tới ngày.</summary>
public sealed record GetCheckInPreviewQuery : IRequest<CheckInPreviewResponse>;

public sealed class GetCheckInPreviewQueryHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider, IOptions<AlgorithmOptions> _options)
    : IRequestHandler<GetCheckInPreviewQuery, CheckInPreviewResponse>
{
    public async Task<CheckInPreviewResponse> Handle(GetCheckInPreviewQuery request, CancellationToken ct)
        => (await CheckInSupport.BuildAsync(_uow, _userContext.UserId, FoodLogSupport.Today(_timeProvider), _options.Value, ct)).Preview;
}

/// <summary>UC-G03: chấp nhận → MacroProgramWeek mới từ hôm nay + WeeklyCheckIn(Accepted).</summary>
public sealed record AcceptCheckInCommand : IRequest<Guid>;

public sealed class AcceptCheckInCommandHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider, IOptions<AlgorithmOptions> _options)
    : IRequestHandler<AcceptCheckInCommand, Guid>
{
    public async Task<Guid> Handle(AcceptCheckInCommand request, CancellationToken ct)
        => await CheckInSupport.DecideAsync(_uow, _userContext.UserId, FoodLogSupport.Today(_timeProvider), _options.Value, accepted: true, ct);
}

/// <summary>UC-G04 (BR-17): từ chối → giữ target cũ, ghi WeeklyCheckIn(Accepted = false).</summary>
public sealed record RejectCheckInCommand : IRequest<Guid>;

public sealed class RejectCheckInCommandHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider, IOptions<AlgorithmOptions> _options)
    : IRequestHandler<RejectCheckInCommand, Guid>
{
    public async Task<Guid> Handle(RejectCheckInCommand request, CancellationToken ct)
        => await CheckInSupport.DecideAsync(_uow, _userContext.UserId, FoodLogSupport.Today(_timeProvider), _options.Value, accepted: false, ct);
}

/// <summary>UC-G06: đổi ngày check-in trong tuần; không sinh lại target.</summary>
public sealed record ChangeCheckInWeekdayCommand(DayOfWeek CheckInWeekday) : IRequest<Unit>;

public sealed class ChangeCheckInWeekdayCommandValidator : AbstractValidator<ChangeCheckInWeekdayCommand>
{
    public ChangeCheckInWeekdayCommandValidator()
        => RuleFor(x => x.CheckInWeekday).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);
}

public sealed class ChangeCheckInWeekdayCommandHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<ChangeCheckInWeekdayCommand, Unit>
{
    public async Task<Unit> Handle(ChangeCheckInWeekdayCommand request, CancellationToken ct)
    {
        var program = await _uow.Repository<MacroProgram>().FindTrackedAsync(p => p.UserId == _userContext.UserId && p.IsActive, ct)
            ?? throw new NotFoundException(ExceptionMessages.NoActiveProgram);
        program.CheckInWeekday = request.CheckInWeekday;
        await _uow.EnsureSaveAsync(ct);
        return Unit.Value;
    }
}

/// <summary>Lịch sử check-in, mới nhất trước.</summary>
public sealed record GetCheckInHistoryQuery : IRequest<IReadOnlyList<CheckInHistoryModel>>;

public sealed class GetCheckInHistoryQueryHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<GetCheckInHistoryQuery, IReadOnlyList<CheckInHistoryModel>>
{
    public async Task<IReadOnlyList<CheckInHistoryModel>> Handle(GetCheckInHistoryQuery request, CancellationToken ct)
        => await _uow.Repository<WeeklyCheckIn>().Query()
            .Where(c => c.UserId == _userContext.UserId)
            .OrderByDescending(c => c.CheckInDate)
            .ProjectToType<CheckInHistoryModel>()
            .ToListAsync(ct);
}
