namespace Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddHttpContextAccessor();

        services.Configure<EmailOptions>(configuration.GetSection("EmailSettings"));
        services.Configure<FrontendOptions>(configuration.GetSection(FrontendOptions.SectionName));
        services.Configure<CloudinaryOptions>(configuration.GetSection("Cloudinary"));
        services.Configure<AlgorithmOptions>(configuration.GetSection(AlgorithmOptions.SectionName));

        services.AddInfrastructureDbContext(configuration)
                .AddJwtService(configuration)
                .AddRedisCache(configuration)
                .AddQuartzService()
                .AddServicesFromAssembly(AssemblyReference.Assembly);   // mọi class có [RegisterService]

        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

        return services;
    }
}
