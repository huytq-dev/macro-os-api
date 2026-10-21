namespace Application;

[RegisterService(typeof(IVerificationEmailSender))]
public sealed class VerificationEmailSender(
    IJwtTokensService _jwt,
    IRedisCacheService _redis,
    IBackgroundJobService _jobs,
    IOptions<FrontendOptions> _frontend
) : IVerificationEmailSender
{
    public const string KeyPrefix = "email-verify:";
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromHours(24);

    public async Task SendAsync(User user, CancellationToken ct = default)
    {
        var token = _jwt.GenerateEmailVerifyToken();
        await _redis.SetRecordAsync($"{KeyPrefix}{token}", user.Id.ToString(), TokenLifetime);

        var link = $"{_frontend.Value.BaseUrl.TrimEnd('/')}/confirm-email?token={token}";
        await _jobs.EnqueueEmailAsync(new EmailContent
        {
            Receiver = user.Email,
            Subject = "Xác thực email MacroOS",
            Body =
                "<p>Bấm vào link bên dưới để xác thực email (có hiệu lực 24 giờ):</p>" +
                $"<p><a href=\"{link}\">Xác thực email</a></p>" +
                $"<p>Nếu link không mở được, dán mã này vào trang xác thực: <b>{token}</b></p>"
        });
    }
}
