namespace Infrastructure;

public interface IBackgroundJobService
{
    Task<string> EnqueueEmailAsync(EmailContent email);

    Task<string> EnqueueEmailAsync(EmailContent email, DateTime scheduleAt);

    Task<bool> CancelJobAsync(string jobId);

    Task<bool> JobExistsAsync(string jobId);

    Task Enqueue(Func<Task> task);
}
