namespace Application;

public sealed class LoginCommandHandler(
    IUnitOfWork _uow,
    IPasswordHasher _hasher,
    IJwtTokensService _jwt
) : IRequestHandler<LoginCommand, SignInResponse>
{
    public async Task<SignInResponse> Handle(LoginCommand request, CancellationToken ct)
    {
        var user = await _uow.Repository<User>().Query(tracking: true)
            .Include(u => u.Profile)
            .FirstOrDefaultAsync(u => u.Email == request.Email, ct)
            ?? throw new UnauthorizedException("Email hoặc mật khẩu không đúng.");

        if (!_hasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Email hoặc mật khẩu không đúng.");

        if (user.EmailVerified == false)
            throw new BadRequestException("Tài khoản chưa xác thực email. Mở email xác thực hoặc gửi lại email mới.");

        var response = _jwt.GenerateAccessToken(user.Adapt<UserCredentials>());
        response.RefreshToken = _jwt.GenerateRefreshToken();
        response.RefreshTokenExpiresAt = _jwt.GetRefreshTokenExpiry();

        user.RefreshToken = response.RefreshToken;
        user.RefreshTokenExpiry = response.RefreshTokenExpiresAt;
        await _uow.EnsureSaveAsync(ct);

        return response;
    }
}
