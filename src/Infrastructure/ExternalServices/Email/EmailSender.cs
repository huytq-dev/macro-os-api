using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Logging;

namespace Infrastructure;

[RegisterService(typeof(IEmailSender))]
public class EmailSender : IEmailSender
{
    private readonly EmailOptions _settings;
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(IOptions<EmailOptions> settings, ILogger<EmailSender> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(EmailContent emailModel)
    {
        try
        {
            var emailMessage = new MimeMessage();

            emailMessage.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));

            emailMessage.To.Add(new MailboxAddress(emailModel.Receiver, emailModel.Receiver));

            emailMessage.Subject = emailModel.Subject;
            var bodyBuilder = new BodyBuilder { HtmlBody = emailModel.Body };
            emailMessage.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();

            await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, SecureSocketOptions.StartTls);

            await client.AuthenticateAsync(_settings.SenderEmail, _settings.AppPassword);

            await client.SendAsync(emailMessage);

            await client.DisconnectAsync(true);

            _logger.LogInformation("Email sent to {Receiver}", emailModel.Receiver);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to {Receiver}", emailModel.Receiver);
            throw;
        }
    }
}