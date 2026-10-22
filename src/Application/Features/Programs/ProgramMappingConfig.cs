namespace Application;

public sealed class ProgramMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<MacroProgramWeek, ProgramWeekModel>()
            .Map(dest => dest.AdjustmentReason, src => src.AdjustmentReason.ToString())
            .Map(dest => dest.TargetProvenance, src => src.DaySnapshotRevision != null ? ProgramTargetResolver.Verified : ProgramTargetResolver.LegacyUnverified);

        config.NewConfig<MacroProgram, ProgramResponse>()
            .Map(dest => dest.ProgramType,    src => src.ProgramType.ToString())
            .Map(dest => dest.GoalType,       src => src.GoalType.ToString())
            .Map(dest => dest.DietStyle,      src => src.DietStyle.ToString())
            .Map(dest => dest.ProteinLevel,   src => src.ProteinLevel.ToString())
            .Map(dest => dest.CheckInWeekday, src => src.CheckInWeekday.ToString())
            .Map(dest => dest.CalorieShiftMode, src => src.CalorieShiftMode.ToString())
            .Map(dest => dest.CalorieShiftDays, src => CalorieShiftCalculator.DayNames(src.CalorieShiftDays))   // cờ DaysOfWeek → danh sách tên thứ
            .Map(dest => dest.Weeks,          src => src.Weeks.OrderByDescending(w => w.EffectiveFrom).ToList())
            .Map(dest => dest.CurrentWeek,    src => src.Weeks.OrderByDescending(w => w.EffectiveFrom).FirstOrDefault());
    }
}
