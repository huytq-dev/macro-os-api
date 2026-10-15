// Domain/Entities/Metrics/BodyMetric.cs
using Domain;

public class BodyMetric : EntityBase<Guid>, ISoftDeletable
{
    public Guid UserId { get; set; }
    public DateOnly MeasuredDate { get; set; }
    public TimeOnly? MeasuredTime { get; set; }
    public MeasurementMethod Method { get; set; }  // Scale, DEXA, Tape, Visual

    // Primary — WeightKg được log riêng qua WeightEntry (daily weigh-in)
    public decimal? HeightCm { get; set; }
    public decimal? Bmi { get; set; }              // Calculated from height + latest weight

    // Body Composition
    public decimal? BodyFatPercent { get; set; }
    public decimal? MuscleMassKg { get; set; }
    public decimal? BoneMassKg { get; set; }
    public decimal? WaterPercent { get; set; }
    public decimal? VisceralFatLevel { get; set; }  // 1-30 scale

    // Circumferences (cm)
    public decimal? WaistCm { get; set; }
    public decimal? HipCm { get; set; }
    public decimal? ChestCm { get; set; }
    public decimal? NeckCm { get; set; }
    public decimal? LeftArmCm { get; set; }
    public decimal? RightArmCm { get; set; }
    public decimal? LeftThighCm { get; set; }
    public decimal? RightThighCm { get; set; }
    public decimal? LeftCalfCm { get; set; }
    public decimal? RightCalfCm { get; set; }

    // Vitals
    public int? RestingHeartRate { get; set; }     // bpm
    public decimal? BloodPressureSystolic { get; set; }
    public decimal? BloodPressureDiastolic { get; set; }

    // Photos
    public string? FrontPhotoUrl { get; set; }
    public string? SidePhotoUrl { get; set; }
    public string? BackPhotoUrl { get; set; }

    public string? Notes { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public User User { get; set; } = null!;
}
