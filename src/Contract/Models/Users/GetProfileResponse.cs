namespace Contract;

/// <param name="CreatedAt">Ngày tạo tài khoản (User.CreatedAt) — FE hiện "Member Since".</param>
public sealed record GetProfileResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string? AvatarUrl,
    string Gender,
    DateOnly? DateOfBirth,
    decimal? HeightCm,
    string ActivityLevel,
    string UnitPreference,
    string? Bio,
    string? TimeZone,
    string? Locale,
    bool EmailVerified,
    DateTimeOffset CreatedAt
);
