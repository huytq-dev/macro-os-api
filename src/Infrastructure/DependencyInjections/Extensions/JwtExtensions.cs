using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Infrastructure;

internal static class JwtExtensions
{
    public static IServiceCollection AddJwtService(this IServiceCollection services, IConfiguration configuration)
    {
        var jwt = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        jwt.EnsureValid();
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        services.AddTransient<CustomJwtBearerEvents>();

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme             = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.SaveToken  = true;
            options.EventsType = typeof(CustomJwtBearerEvents);
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer           = true,
                ValidateAudience         = true,
                ValidateLifetime         = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer              = jwt.Issuer,
                ValidAudience            = jwt.Audience,
                IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey)),
                ClockSkew                = TimeSpan.Zero
            };
        });

        services.AddAuthorization();

        return services;
    }
}
