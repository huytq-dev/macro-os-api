namespace Application;

public sealed record UpdateProfileRequest(
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
);
