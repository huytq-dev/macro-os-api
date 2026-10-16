using TriggerBuilder = Quartz.TriggerBuilder;

namespace Infrastructure;

[RegisterService(typeof(IBackgroundJobService))]
public class QuartzBackgroundJobService : IBackgroundJobService
{
    private readonly IScheduler _scheduler;

    public QuartzBackgroundJobService(ISchedulerFactory schedulerFactory)
    {
        _scheduler = schedulerFactory.GetScheduler().GetAwaiter().GetResult();

        if (!_scheduler.IsStarted)
            _scheduler.Start().GetAwaiter().GetResult();
    }

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

        await _scheduler.ScheduleJob(job, trigger);
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

        await _scheduler.ScheduleJob(job, trigger);
        return jobId;
    }

    public async Task<bool> CancelJobAsync(string jobId)
    {
        var jobKey = new JobKey(jobId, "email-group");
        return await _scheduler.CheckExists(jobKey) && await _scheduler.DeleteJob(jobKey);
    }

    public async Task<bool> JobExistsAsync(string jobId)
    {
        var jobKey = new JobKey(jobId, "email-group");
        return await _scheduler.CheckExists(jobKey);
    }

    public Task Enqueue(Func<Task> task)
    {
        _ = Task.Run(task);
        return Task.CompletedTask;
    }
}