namespace Contract;

public interface IAppConfiguration
{
    JwtOptions GetJwtOptions();
    CloudinaryOptions GetCloudinaryOptions();
    //string? GetSqlServerConnectionString();
    //public string? GetEnvironment();
}
