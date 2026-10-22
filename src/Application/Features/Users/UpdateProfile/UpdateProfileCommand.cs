namespace Application;

public sealed record UpdateProfileCommand(
    string? FirstName,
    string? LastName,
    DateOnly? DateOfBirth,
    Gender? Gender,
    decimal? HeightCm,
    ActivityLevel? ActivityLevel,
    UnitSystem? UnitPreference,
    string? Bio,
    string? TimeZone,
    string? Locale,
    EnergyUnit? EnergyUnit = null,
    DayOfWeek? WeekStartDay = null
) : IRequest<Unit>;
