namespace Application;

/// <param name="RateTargetKg">kg/tuần mục tiêu tại cân hiện tại (dương = giảm).</param>
public sealed record CheckInProposal(
    int PreviousCalories,
    int RawCalories,
    int NewCalories,
    int Delta,
    bool Clamped,
    bool FloorApplied,
    decimal RateTargetKg,
    MacroTargets Targets
);
