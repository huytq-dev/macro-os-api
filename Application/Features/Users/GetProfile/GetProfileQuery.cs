namespace Application;

public sealed record GetProfileQuery(Guid UserId) : IRequest<GetProfileResponse>;
