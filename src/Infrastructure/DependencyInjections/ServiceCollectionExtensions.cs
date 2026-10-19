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
        services.Configure<UsdaOptions>(configuration.GetSection(UsdaOptions.SectionName));
        services.Configure<AiOptions>(configuration.GetSection(AiOptions.SectionName));
        services.Configure<ProgressPhotoOptions>(configuration.GetSection(ProgressPhotoOptions.SectionName));

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

        // Phân tích bữa ăn bằng AI: timeout lấy từ Ai:TimeoutSeconds; khoá API gắn theo từng request (trống → handler trả 503)
        services.AddHttpClient<IAiMealAnalyzer, AnthropicMealAnalyzer>((sp, c) =>
        {
            c.BaseAddress = new Uri("https://api.anthropic.com/");
            c.Timeout = TimeSpan.FromSeconds(sp.GetRequiredService<IOptions<AiOptions>>().Value.TimeoutSeconds);
            c.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        });

        // USDA FoodData Central (importer WP1): theo lô nhỏ nên timeout rộng hơn OFF
        services.AddHttpClient<IUsdaClient, UsdaClient>(c =>
        {
            c.BaseAddress = new Uri("https://api.nal.usda.gov/fdc/v1/");
            c.Timeout = TimeSpan.FromSeconds(20);
        });

        return services;
    }
}
