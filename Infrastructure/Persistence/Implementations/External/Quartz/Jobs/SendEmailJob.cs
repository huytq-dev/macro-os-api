using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

[RegisterService(typeof(IJob), ServiceLifetime.Transient)]
public class SendEmailJob(IEmailSender _emailSender) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var data = context.JobDetail.JobDataMap;
        var email = new EmailContent
        {
            Receiver = data.GetString("to")!,
            Subject = data.GetString("subject")!,
            Body = data.GetString("body")!
        };

        await _emailSender.SendEmailAsync(email);
    }
}
