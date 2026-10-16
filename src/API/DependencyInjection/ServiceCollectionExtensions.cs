using Microsoft.OpenApi;

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
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "MacroOS API", Version = "v1" });
            // Nút Authorize: dán access token (không cần chữ "Bearer") để thử các API cần đăng nhập
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Access token từ POST /api/auth/login (dán token, không kèm chữ Bearer)."
            });
            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = []
            });
        });

        services.AddApiHealthChecks();

        services.AddCORSPolicy(configuration)
                .AddRateLimiting(configuration)
                .AddProblemDetails()
                .AddExceptionHandler<GlobalExceptionHandler>();

        return services;
    }
}
