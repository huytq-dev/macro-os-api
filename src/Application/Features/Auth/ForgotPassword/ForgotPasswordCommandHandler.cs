namespace Application;

public sealed class ForgotPasswordCommandHandler(
    IUnitOfWork _uow,
    IJwtTokensService _jwt,
    IRedisCacheService _redis,
    IBackgroundJobService _jobs
) : IRequestHandler<ForgotPasswordCommand, Unit>
{
    public async Task<Unit> Handle(ForgotPasswordCommand request, CancellationToken ct)
    {
        var user = await _uow.Repository<User>().FindAsync(u => u.Email == request.Email, ct);

        // Không tiết lộ email có tồn tại hay không
        if (user is null) return Unit.Value;

        var token = _jwt.GenerateEmailVerifyToken();
        var redisKey = $"reset-password:{token}";
        await _redis.SetRecordAsync(redisKey, user.Id.ToString(), TimeSpan.FromMinutes(15));

        await _jobs.EnqueueEmailAsync(new EmailContent
        {
            Receiver = user.Email,
            Subject = "Đặt lại mật khẩu MacroOS",
            Body = $"Token đặt lại mật khẩu của bạn: <b>{token}</b><br/>Token có hiệu lực trong 15 phút."
        });

        return Unit.Value;
    }
}
