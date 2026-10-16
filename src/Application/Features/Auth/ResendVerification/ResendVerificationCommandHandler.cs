namespace Application;

public sealed class ResendVerificationCommandHandler(
    IUnitOfWork _uow,
    IRedisCacheService _redis,
    IVerificationEmailSender _verificationEmail
) : IRequestHandler<ResendVerificationCommand, Unit>
{
    private static readonly TimeSpan Cooldown = TimeSpan.FromSeconds(60);

    public async Task<Unit> Handle(ResendVerificationCommand request, CancellationToken ct)
    {
        var user = await _uow.Repository<User>().FindAsync(u => u.Email == request.Email, ct);

        // Không tiết lộ email có tồn tại hay đã xác thực chưa
        if (user is null || user.EmailVerified == true) return Unit.Value;

        // Chống spam hộp thư: tối đa một email mỗi 60 giây cho mỗi tài khoản
        var cooldownKey = $"email-verify-cooldown:{user.Id}";
        if (await _redis.GetRecordAsync<string>(cooldownKey) is not null) return Unit.Value;
        await _redis.SetRecordAsync(cooldownKey, "1", Cooldown);

        await _verificationEmail.SendAsync(user, ct);
        return Unit.Value;
    }
}
