namespace Application;

public sealed class GetProfileQueryHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<GetProfileQuery, GetProfileResponse>
{
    public async Task<GetProfileResponse> Handle(GetProfileQuery request, CancellationToken ct)
    {
        var user = await _uow.Repository<User>().FindAsync(u => u.Id == _userContext.UserId, ct, u => u.Profile)
            ?? throw new NotFoundException(ExceptionMessages.NotFound("User", _userContext.UserId));

        return user.Adapt<GetProfileResponse>();
    }
}
