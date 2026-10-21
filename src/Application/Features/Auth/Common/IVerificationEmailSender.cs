namespace Application;

/// <summary>Tạo token xác thực email (một lần, 24 giờ), lưu Redis và gửi mail có link tới FE.</summary>
public interface IVerificationEmailSender
{
    Task SendAsync(User user, CancellationToken ct = default);
}
