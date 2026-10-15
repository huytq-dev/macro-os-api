namespace Application;

public sealed class LoginCommandHandler(
    IUnitOfWork _uow,
    IPasswordHasher _hasher,
    IJwtTokensService _jwt,
    IMapper _mapper
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
            throw new BadRequestException("Tài khoản chưa được xác thực email.");

        var response = _jwt.GenerateAccessToken(_mapper.Map<UserCredentials>(user));
        response.RefreshToken = _jwt.GenerateRefreshToken();

        user.RefreshToken = response.RefreshToken;
        user.RefreshTokenExpiry = DateTimeOffset.UtcNow.AddDays(30);
        await _uow.EnsureSaveAsync(ct);

        return response;
    }
}
