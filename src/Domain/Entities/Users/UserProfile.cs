namespace Domain;

public class UserProfile : EntityBase<Guid>
{
    public Guid UserId { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public DateOnly? DateOfBirth { get; set; }
    public Gender Gender { get; set; }
    public UnitSystem UnitPreference { get; set; } = UnitSystem.Metric;
    public decimal? HeightCm { get; set; }
    public ActivityLevel ActivityLevel { get; set; } = ActivityLevel.ModeratelyActive;
    public string? Bio { get; set; }
    public string? TimeZone { get; set; }
    public string? Locale { get; set; }

    // Navigation
    public User User { get; set; } = null!;
}
