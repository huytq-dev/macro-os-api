namespace Application;

/// <summary>
/// D2 (PCI-005): chụp 7 target ngày HIỆU LỰC của một phiên bản <see cref="MacroProgramWeek"/> (đã áp dòng ngày, ngày nhịn ăn, dịch kcal) vào <see cref="MacroProgramWeekDayTarget"/>.
/// Chỉ <see cref="ProgramWeekBuilder.UpsertWeek"/> gọi (điểm ghi tuần duy nhất): snapshot nằm cùng giao dịch SaveChanges với dòng tuần.
/// Quy tắc cập nhật: một phiên bản tuần KHÔNG BAO GIỜ sửa/xoá snapshot cũ; mỗi lần ghi lại (kể cả cùng ngày hiệu lực: check-in rồi sửa goal trong cùng ngày) thêm bộ 7 dòng mới với
/// <see cref="MacroProgramWeek.DaySnapshotRevision"/> + 1, bộ cũ giữ làm audit target trước đó (PCI-006). Tuần mới (EffectiveFrom mới) bắt đầu từ revision 1.
/// </summary>
public static class WeekDaySnapshots
{
    public static TargetDistribution DistributionOf(MacroProgram program)
        => program.DayTargets.Count > 0 ? TargetDistribution.DayRows
         : !CalorieShiftCalculator.Applies(program.CalorieShiftMode, program.CalorieShiftPercent, program.CalorieShiftDays) ? TargetDistribution.Flat
         : program.CalorieShiftMode == CalorieShiftMode.TrainingDays ? TargetDistribution.TrainingDays : TargetDistribution.Custom;

    /// <summary>Gọi SAU khi target tuần và dòng ngày đã ở trạng thái cuối (sau Rebalance). Không gán Id/MacroProgramWeekId: EF sinh khi INSERT (xem ghi chú UpsertWeek).</summary>
    public static void Capture(MacroProgram program, MacroProgramWeek week, AdjustmentReason reason)
    {
        var revision = (week.DaySnapshotRevision ?? 0) + 1;
        var distribution = DistributionOf(program);
        foreach (var day in ProgramTargetResolver.WeekOrder)
        {
            var t = ProgramTargetResolver.Live(week, program, day);
            var row = program.DayTargets.FirstOrDefault(d => d.DayOfWeek == day);
            week.DayTargetSnapshots.Add(new MacroProgramWeekDayTarget
            {
                SnapshotRevision = revision,
                DayOfWeek = day,
                IsFastingDay = row?.IsFastingDay ?? false,
                IsLocked = row?.IsLocked ?? false,
                DayType = row?.DayType ?? DayType.Standard,
                Distribution = distribution,
                AdjustmentReason = reason,
                AdditionalTargetsJson = row?.AdditionalTargetsJson,
                CaloriesTarget = t.CaloriesTarget,
                ProteinTarget = t.ProteinTarget,
                CarbsTarget = t.CarbsTarget,
                FatTarget = t.FatTarget,
            });
        }
        week.DaySnapshotRevision = revision;
    }
}
