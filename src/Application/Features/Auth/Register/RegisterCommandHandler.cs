namespace Application;

public sealed class RegisterCommandHandler(
    IUnitOfWork _uow,
    IPasswordHasher _hasher,
    IJwtTokensService _jwt,
    IBackgroundJobService _jobs
) : IRequestHandler<RegisterCommand, string>
{
    public async Task<string> Handle(RegisterCommand request, CancellationToken ct)
    {
        var exists = await _uow.Repository<User>().AnyAsync(u => u.Email == request.Email, ct);

        if (exists)
            throw new ConflictException(ExceptionMessages.AlreadyExists("Email", request.Email));

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            PasswordHash = _hasher.Hash(request.Password),
            EmailVerified = false,
            Role = UserRole.Vip
        };

        user.Profile = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Gender = request.Gender
        };

        await _uow.Repository<User>().AddAsync(user, ct);
        await _uow.EnsureSaveAsync(ct);

        var verifyToken = _jwt.GenerateEmailVerifyToken();
        await _jobs.EnqueueEmailAsync(new EmailContent
        {
            Receiver = user.Email,
            Subject = "Xác thực email MacroOS",
            Body = $"Mã xác thực của bạn: <b>{verifyToken}</b>"
        });

        return "Đăng ký thành công. Vui lòng kiểm tra email để xác thực tài khoản.";
    }
}
