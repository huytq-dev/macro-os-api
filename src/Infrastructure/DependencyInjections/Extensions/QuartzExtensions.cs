namespace Infrastructure;

internal static class QuartzExtensions
{
    public static IServiceCollection AddQuartzService(this IServiceCollection services)
    {
        services.AddQuartz(q =>
        {
            q.SchedulerId   = "AUTO";
            q.SchedulerName = "MacroOSScheduler";
            q.UseDefaultThreadPool(tp => tp.MaxConcurrency = 5);
            q.UseInMemoryStore();
            // Email job tạo động trong QuartzBackgroundJobService.EnqueueEmailAsync(); job lịch cố định đăng ký ở đây
            var expenditureKey = new JobKey(nameof(ExpenditureNightlyJob));
            q.AddJob<ExpenditureNightlyJob>(o => o.WithIdentity(expenditureKey));
            q.AddTrigger(t => t.ForJob(expenditureKey)
                .WithIdentity($"{nameof(ExpenditureNightlyJob)}-trigger")
                .WithCronSchedule(ExpenditureNightlyJob.CronUtc, c => c.InTimeZone(TimeZoneInfo.Utc)));
        });

        services.AddQuartzHostedService(o => o.WaitForJobsToComplete = true);

        return services;
    }
}
