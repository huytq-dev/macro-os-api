using Microsoft.Extensions.Configuration;

namespace Contract;

public class AppConfiguration(IConfiguration configuration) : IAppConfiguration
{

    public JwtOptions GetJwtOptions()
    {
        var jwtOptions = configuration.GetSection("Jwt").Get<JwtOptions>();

        if (jwtOptions == null)
            throw new InvalidOperationException("Missing 'Jwt' section in appsettings.json");

        // 3. Validate dữ liệu quan trọng
        // Đảm bảo không có trường nào bị rỗng
        if (string.IsNullOrWhiteSpace(jwtOptions.Key) ||
            string.IsNullOrWhiteSpace(jwtOptions.Issuer) ||
            string.IsNullOrWhiteSpace(jwtOptions.Audience))
        {
            throw new InvalidOperationException("Jwt configuration is invalid. Key, Issuer, and Audience are required.");
        }

        return jwtOptions;
    }

    public CloudinaryOptions GetCloudinaryOptions()
    {
        var cloudinaryOptions = configuration.GetSection("Cloudinary").Get<CloudinaryOptions>();

        if (cloudinaryOptions == null)
            throw new InvalidOperationException("Missing 'Cloudinary' section in appsettings.json");

        return cloudinaryOptions;
    }

    //public string? GetSqlServerConnectionString()
    // => _configuration.GetConnectionString(AppConstants.SqlServerConnection) ?? throw new Exception("An unexpected error occurred.");

    //public string? GetSqlServerForDapperConnectionString()
    // => _configuration.GetConnectionString(AppConstants.SqlServerForDapperConnection) ?? throw new Exception("An unexpected error occurred.");

    //public string? GetEnvironment()
    //{
    //    return _hostEnvironment.EnvironmentName;
    //}
}
