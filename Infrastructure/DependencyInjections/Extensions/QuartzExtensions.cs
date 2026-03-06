namespace Infrastructure;

internal static class QuartzExtensions
{
    public static IServiceCollection AddQuartzService(this IServiceCollection services, IConfiguration configuration)
    {
        // Cấu hình Quartz với In-Memory store (lưu trong RAM)
        services.AddQuartz(q =>
        {
            // Use DI to construct jobs with dependencies (e.g., SendEmailJob -> IEmailSender)
            q.SchedulerId = "AUTO";
            q.SchedulerName = "FEventScheduler";

            q.UseDefaultThreadPool(tp => { tp.MaxConcurrency = 5; });

            // Sử dụng In-Memory store (jobs sẽ mất khi ứng dụng restart)
            q.UseInMemoryStore();

            // Không cần đăng ký job trước vì job được tạo động trong QuartzBackgroundJobService
            // khi gọi EnqueueEmailAsync()
        });

        // Đăng ký Quartz Hosted Service để chạy scheduler
        services.AddQuartzHostedService(options =>
        {
            options.WaitForJobsToComplete = true;
        });
        return services;
    }
}
