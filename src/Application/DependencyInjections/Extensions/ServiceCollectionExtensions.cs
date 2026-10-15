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
                    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(TrackingPipelineBehavior<,>));
                });

        return services;
    }
}
