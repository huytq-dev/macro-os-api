namespace Application;

public sealed class RefreshTokenCommandHandler(
    IUnitOfWork _uow,
    IJwtTokensService _jwt,
    IMapper _mapper
) : IRequestHandler<RefreshTokenCommand, SignInResponse>
{
    public async Task<SignInResponse> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        var principal = _jwt.GetPrincipalFromExpiredToken(request.ExpiredAccessToken);

        var userIdStr = principal.FindFirst("sub")?.Value
            ?? throw new UnauthorizedException(ExceptionMessages.Invalid("Token"));

        if (!Guid.TryParse(userIdStr, out var userId))
            throw new UnauthorizedException(ExceptionMessages.Invalid("Token"));

        var user = await _uow.Repository<User>().Query(tracking: true)
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new UnauthorizedException(ExceptionMessages.NotFound("User", userId));

        if (user.RefreshToken != request.RefreshToken || user.RefreshTokenExpiry <= DateTimeOffset.UtcNow)
            throw new UnauthorizedException(ExceptionMessages.Invalid("Refresh token"));

        var response = _jwt.GenerateAccessToken(_mapper.Map<UserCredentials>(user));
        response.RefreshToken = _jwt.GenerateRefreshToken();

        user.RefreshToken = response.RefreshToken;
        user.RefreshTokenExpiry = DateTimeOffset.UtcNow.AddDays(30);
        await _uow.EnsureSaveAsync(ct);

        return response;
    }
}
