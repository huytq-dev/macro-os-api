namespace Application;

public sealed class DeleteNutritionDataCommandHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider, IOptions<AlgorithmOptions> _options)
    : IRequestHandler<DeleteNutritionDataCommand, Unit>
{
    public async Task<Unit> Handle(DeleteNutritionDataCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var log = await _uow.Repository<FoodLog>().Query(tracking: true).Include(l => l.Entries)
            .FirstOrDefaultAsync(l => l.UserId == userId && l.LogDate == request.Date, ct)
            ?? throw new NotFoundException(ExceptionMessages.NotFound("FoodLog", request.Date));
        if (NutritionDataSupport.IsAppDay(log.Entries.Where(e => !e.IsDeleted)))
            throw new BadRequestException(ExceptionMessages.DayLoggedInFoodLogDelete);

        await _uow.Repository<FoodLog>().RemoveAsync(log, ct);
        await FoodLogSupport.SaveAndRebuildAsync(_uow, userId, request.Date, _options.Value, _timeProvider, ct);
        return Unit.Value;
    }
}
