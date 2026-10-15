namespace API;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                    // Enum đi/về dạng chuỗi ("WeightLoss") để FE dùng union string, không map số
                    options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
                });

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        services.AddCORSPolicy(configuration)
                .AddRateLimiting(configuration)
                .AddProblemDetails()
                .AddExceptionHandler<GlobalExceptionHandler>();

        return services;
    }
}
