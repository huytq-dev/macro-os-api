namespace Application;

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
