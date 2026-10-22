namespace Application;

public sealed class SetExpenditureModeCommandHandler(IUnitOfWork _uow, IUserContext _userContext, IOptions<AlgorithmOptions> _options, TimeProvider _timeProvider)
    : IRequestHandler<SetExpenditureModeCommand, ExpenditureSettingsResponse>
{
    public async Task<ExpenditureSettingsResponse> Handle(SetExpenditureModeCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var today = (await UserLocale.GetAsync(_uow, userId, _timeProvider, ct)).Today;

        if (ExpenditureModeTimeline.Validate(request.Mode, request.StaticTdeeKcal, today, today) is { } error)
            throw new BadRequestException(error);

        // Cùng khoá với dựng lại dẫn xuất: chuyển chế độ không xen vào giữa một lần dựng, hai lần bấm đồng thời xếp hàng tuần tự
        await _uow.BeginTransactionAsync(ct);
        try
        {
            if (!await _uow.TryAcquireTransactionLockAsync(DerivedDataSync.LockResource(userId), _options.Value.DerivedLockWaitMs, ct))
                throw new ConflictException(ExceptionMessages.ExpenditureSettingsBusy);

            var repo = _uow.Repository<ExpenditureModePeriod>();
            var periodToday = await repo.FindTrackedAsync(p => p.UserId == userId && p.EffectiveFrom == today, ct);
            if (periodToday is null)
            {
                periodToday = new ExpenditureModePeriod { Id = Guid.NewGuid(), UserId = userId, EffectiveFrom = today };
                await repo.AddAsync(periodToday, ct);
            }
            periodToday.Mode = request.Mode;
            periodToday.StaticTdeeKcal = request.Mode == ExpenditureMode.Static ? request.StaticTdeeKcal : null;

            await _uow.SaveChangesAsync(ct);
            await _uow.CommitTransactionAsync(ct);
        }
        catch
        {
            try { await _uow.RollbackTransactionAsync(CancellationToken.None); } catch { /* transaction có thể đã đóng */ }
            _uow.DiscardChanges();
            throw;
        }

        return await ExpenditureSettingsSupport.BuildResponseAsync(_uow, userId, today, ct);
    }
}
