namespace Application;

public sealed class GetProfileQueryHandler(
    IUnitOfWork _uow,
    IMapper _mapper
) : IRequestHandler<GetProfileQuery, GetProfileResponse>
{
    public async Task<GetProfileResponse> Handle(GetProfileQuery request, CancellationToken ct)
    {
        var user = await _uow.Repository<User>().FindAsync(u => u.Id == request.UserId, ct, u => u.Profile)
            ?? throw new NotFoundException(ExceptionMessages.NotFound("User", request.UserId));

        return _mapper.Map<GetProfileResponse>(user);
    }
}
