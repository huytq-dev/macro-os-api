namespace Application;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration config)
    {
        var appAssembly = AssemblyReference.Assembly;

        services.AddServicesFromAssembly(appAssembly)
                .AddValidatorsFromAssembly(appAssembly)
                .AddMapsterServices()
                .AddMediatR(cfg =>
                {
                    cfg.RegisterServicesFromAssembly(appAssembly);
                    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationPipelineBehavior<,>));
                    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(PerformancePipelineBehavior<,>));
                    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(FoodLogWriteLockBehavior<,>));   // trong cùng: lỗi validate không giữ khoá; ghi Food Log của một user chạy lần lượt
                });

        return services;
    }
}
