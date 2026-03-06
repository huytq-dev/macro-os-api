namespace Infrastructure;

public interface IEmailSender
{
    Task SendEmailAsync(EmailContent email);
}
