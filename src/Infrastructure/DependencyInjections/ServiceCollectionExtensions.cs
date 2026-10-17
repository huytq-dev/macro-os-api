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

        // Tra mã vạch (UC-C08): timeout ngắn để quét không treo; OFF yêu cầu User-Agent nhận diện app
        services.AddHttpClient<IOpenFoodFactsClient, OpenFoodFactsClient>(c =>
        {
            c.BaseAddress = new Uri("https://world.openfoodfacts.org/");
            c.Timeout = TimeSpan.FromSeconds(5);
            c.DefaultRequestHeaders.UserAgent.ParseAdd("MacroOS/1.0 (personal app)");
        });

        return services;
    }
}
