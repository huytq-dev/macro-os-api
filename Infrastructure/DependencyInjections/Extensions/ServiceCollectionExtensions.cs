namespace Infrastructure;

public static class ServiceCollectionExtensions 
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var appConfig = new AppConfiguration(configuration);
        var jwtOptions = appConfig.GetJwtOptions();

        services.AddSingleton<IAppConfiguration>(appConfig);
        services.Configure<EmailOptions>(configuration.GetSection("EmailSettings"));

        services.AddSingleton(Options.Create(jwtOptions));

        services.AddInfrastructureDbContext(configuration)
                .AddJwtService(jwtOptions)
                .AddRedisCache(configuration)
                .AddQuartzService(configuration)
                .AddCloudinaryService(configuration);

        // Quét và đăng ký các class có attribute [RegisterService]
        services.AddServicesFromAssembly(typeof(AssemblyReference).Assembly);

        return services;
    }
}
