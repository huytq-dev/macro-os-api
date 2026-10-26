namespace Application;

public sealed class SetDayIncompleteCommandHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider, IOptions<AlgorithmOptions> _options)
    : IRequestHandler<SetDayIncompleteCommand, NutritionDataDayModel>
{
    public async Task<NutritionDataDayModel> Handle(SetDayIncompleteCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var log = await _uow.Repository<FoodLog>().Query(tracking: true).Include(l => l.Entries)
            .FirstOrDefaultAsync(l => l.UserId == userId && l.LogDate == request.Date, ct);   // không tạo ngày mới
        if (log is null)
            throw request.IsIncomplete ? new BadRequestException(ExceptionMessages.IncompleteNeedsLoggedDay) : new NotFoundException(ExceptionMessages.NotFound("FoodLog", request.Date));

        var live = NutritionDataSupport.Live(log);
        if (request.IsIncomplete && NutritionDataSupport.IsEmptyDay(log, live)) throw new BadRequestException(ExceptionMessages.IncompleteNeedsLoggedDay);

        if (log.IsIncomplete != request.IsIncomplete)
        {
            log.IsIncomplete = request.IsIncomplete;
            await FoodLogSupport.SaveAndRebuildAsync(_uow, userId, request.Date, _options.Value, _timeProvider, ct);
        }
        return NutritionDataSupport.ToModel(log, live);
    }
}
