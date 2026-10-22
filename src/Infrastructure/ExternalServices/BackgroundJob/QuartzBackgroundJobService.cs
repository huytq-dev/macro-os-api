using TriggerBuilder = Quartz.TriggerBuilder;

namespace Infrastructure;

[RegisterService(typeof(IBackgroundJobService))]
public class QuartzBackgroundJobService : IBackgroundJobService
{
    // Scheduler lấy và khởi động bất đồng bộ ở lần dùng đầu (không chặn constructor)
    private readonly Lazy<Task<IScheduler>> _scheduler;

    public QuartzBackgroundJobService(ISchedulerFactory schedulerFactory)
        => _scheduler = new(async () =>
        {
            var scheduler = await schedulerFactory.GetScheduler();
            if (!scheduler.IsStarted) await scheduler.Start();
            return scheduler;
        });

    public async Task<string> EnqueueEmailAsync(EmailContent email)
    {
        var jobId = Guid.NewGuid().ToString();

        var job = JobBuilder.Create<SendEmailJob>()
            .WithIdentity(jobId, "email-group")
            .UsingJobData("to", email.Receiver)
            .UsingJobData("subject", email.Subject)
            .UsingJobData("body", email.Body)
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity($"trigger-{jobId}", "email-group")
            .StartNow()
            .Build();

        await (await _scheduler.Value).ScheduleJob(job, trigger);
        return jobId;
    }

    public async Task<string> EnqueueEmailAsync(EmailContent email, DateTime scheduleAt)
    {
        var jobId = Guid.NewGuid().ToString();

        var job = JobBuilder.Create<SendEmailJob>()
            .WithIdentity(jobId, "email-group")
            .UsingJobData("to", email.Receiver)
            .UsingJobData("subject", email.Subject)
            .UsingJobData("body", email.Body)
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity($"trigger-{jobId}", "email-group")
            .StartAt(scheduleAt)
            .Build();

        await (await _scheduler.Value).ScheduleJob(job, trigger);
        return jobId;
    }

    public async Task<bool> CancelJobAsync(string jobId)
    {
        var jobKey = new JobKey(jobId, "email-group");
        var scheduler = await _scheduler.Value;
        return await scheduler.CheckExists(jobKey) && await scheduler.DeleteJob(jobKey);
    }

    public async Task<bool> JobExistsAsync(string jobId)
    {
        var jobKey = new JobKey(jobId, "email-group");
        return await (await _scheduler.Value).CheckExists(jobKey);
    }
}