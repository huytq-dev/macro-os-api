namespace Application;

public sealed class RegisterCommandHandler(
    IUnitOfWork _uow,
    IPasswordHasher _hasher,
    IVerificationEmailSender _verificationEmail,
    ILogger<RegisterCommandHandler> _logger
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

        try
        {
            await _verificationEmail.SendAsync(user, ct);
        }
        catch (Exception ex)
        {
            // Tài khoản đã lưu; lỗi hạ tầng (Redis, hàng đợi mail) không được làm đăng ký báo thất bại.
            // Người dùng gửi lại email xác thực ở trang xác thực.
            _logger.LogError(ex, "Could not send verification email for new user {UserId}", user.Id);
        }

        return "Đăng ký thành công. Vui lòng kiểm tra email để xác thực tài khoản.";
    }
}
