namespace Application;

/// <summary>Đổi tốc độ/loại goal của program đang chạy (hàm thuần, handler và test dùng chung): dấu rate theo <see cref="GoalRules.SignedRate"/>, kg/tuần quy từ % × cân.</summary>
public static class ProgramGoalUpdate
{
    /// <summary>
    /// Gán <c>WeeklyWeightChangeTargetPercent</c> (dương = giảm, âm = tăng) và <c>WeeklyWeightChangeTargetKg</c> (dương = tăng) theo <c>program.GoalType</c> hiện tại.
    /// Độ lớn = <paramref name="requestedPercent"/> nếu có, không thì giữ độ lớn cũ của program (đổi loại mà không đổi tốc độ).
    /// </summary>
    public static void Resign(MacroProgram program, decimal? requestedPercent, decimal weightKg)
    {
        var magnitude = requestedPercent ?? Math.Abs(program.WeeklyWeightChangeTargetPercent);
        program.WeeklyWeightChangeTargetPercent = GoalRules.SignedRate(program.GoalType, magnitude);
        program.WeeklyWeightChangeTargetKg = Math.Round(-program.WeeklyWeightChangeTargetPercent / 100m * weightKg, 2);
    }
}
