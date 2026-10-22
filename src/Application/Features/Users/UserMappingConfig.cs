using Mapster;

namespace Application;

public sealed class UserMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<User, UserCredentials>()
            .Map(dest => dest.FullName,    src => $"{src.Profile.FirstName} {src.Profile.LastName}")
            .Map(dest => dest.AvatarURL,   src => src.AvatarUrl ?? string.Empty)
            .Map(dest => dest.IsVerified,  src => src.EmailVerified ?? false)
            .Map(dest => dest.Role,        src => src.Role.ToString());

        config.NewConfig<User, GetProfileResponse>()
            .Map(dest => dest.FirstName,      src => src.Profile.FirstName)
            .Map(dest => dest.LastName,       src => src.Profile.LastName)
            .Map(dest => dest.Gender,         src => src.Profile.Gender.ToString())
            .Map(dest => dest.DateOfBirth,    src => src.Profile.DateOfBirth)
            .Map(dest => dest.HeightCm,       src => src.Profile.HeightCm)
            .Map(dest => dest.ActivityLevel,  src => src.Profile.ActivityLevel.ToString())
            .Map(dest => dest.UnitPreference, src => src.Profile.UnitPreference.ToString())
            .Map(dest => dest.Bio,            src => src.Profile.Bio)
            .Map(dest => dest.TimeZone,       src => src.Profile.TimeZone)
            .Map(dest => dest.Locale,         src => src.Profile.Locale)
            .Map(dest => dest.EmailVerified,  src => src.EmailVerified ?? false)
            .Map(dest => dest.EnergyUnit,     src => src.Profile.EnergyUnit.ToString())
            .Map(dest => dest.WeekStartDay,   src => src.Profile.WeekStartDay.ToString());
    }
}
