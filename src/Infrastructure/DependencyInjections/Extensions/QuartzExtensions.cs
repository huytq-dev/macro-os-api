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
            // Job được tạo động trong QuartzBackgroundJobService.EnqueueEmailAsync(), không đăng ký trước
        });

        services.AddQuartzHostedService(o => o.WaitForJobsToComplete = true);

        return services;
    }
}
