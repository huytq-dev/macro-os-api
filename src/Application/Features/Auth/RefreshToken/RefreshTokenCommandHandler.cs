namespace Application;

public sealed class RefreshTokenCommandHandler(
    IUnitOfWork _uow,
    IJwtTokensService _jwt,
    TimeProvider _timeProvider
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

        if (string.IsNullOrEmpty(request.RefreshToken) || user.RefreshToken != request.RefreshToken || user.RefreshTokenExpiry <= _timeProvider.GetUtcNow())
            throw new UnauthorizedException(ExceptionMessages.Invalid("Refresh token"));

        var response = _jwt.GenerateAccessToken(user.Adapt<UserCredentials>());
        response.RefreshToken = _jwt.GenerateRefreshToken();
        response.RefreshTokenExpiresAt = _jwt.GetRefreshTokenExpiry();

        user.RefreshToken = response.RefreshToken;
        user.RefreshTokenExpiry = response.RefreshTokenExpiresAt;
        await _uow.EnsureSaveAsync(ct);

        return response;
    }
}
