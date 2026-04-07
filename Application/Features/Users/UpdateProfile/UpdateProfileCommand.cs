namespace Application;

public sealed record UpdateProfileCommand(
    Guid UserId,
    string? FirstName,
    string? LastName,
    DateOnly? DateOfBirth,
    Gender? Gender,
    decimal? HeightCm,
    ActivityLevel? ActivityLevel,
    UnitSystem? UnitPreference,
    string? Bio,
    string? TimeZone,
    string? Locale
) : IRequest<Unit>;
