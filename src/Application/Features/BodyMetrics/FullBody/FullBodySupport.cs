namespace Application;

/// <summary>Chuyển qua lại giữa hàng Tape của BodyMetric và <see cref="TapeMeasurementsModel"/>; hàm thuần.</summary>
public static class FullBodySupport
{
    public static TapeMeasurementsModel ToModel(BodyMetric m) => new(
        m.NeckCm, m.ShouldersCm, m.BustCm, m.ChestCm, m.WaistCm, m.HipCm, m.LeftArmCm, m.RightArmCm, m.LeftForearmCm, m.RightForearmCm,
        m.LeftWristCm, m.RightWristCm, m.LeftThighCm, m.RightThighCm, m.LeftCalfCm, m.RightCalfCm, m.LeftAnkleCm, m.RightAnkleCm);

    /// <summary>Ghi MỌI ô (null = xoá ô đó): sheet gửi nguyên trạng thái, nên ô user bỏ trống phải được xoá.</summary>
    public static void Apply(BodyMetric m, TapeMeasurementsModel t)
    {
        m.NeckCm = t.NeckCm; m.ShouldersCm = t.ShouldersCm; m.BustCm = t.BustCm; m.ChestCm = t.ChestCm; m.WaistCm = t.WaistCm; m.HipCm = t.HipCm;
        m.LeftArmCm = t.LeftArmCm; m.RightArmCm = t.RightArmCm; m.LeftForearmCm = t.LeftForearmCm; m.RightForearmCm = t.RightForearmCm;
        m.LeftWristCm = t.LeftWristCm; m.RightWristCm = t.RightWristCm; m.LeftThighCm = t.LeftThighCm; m.RightThighCm = t.RightThighCm;
        m.LeftCalfCm = t.LeftCalfCm; m.RightCalfCm = t.RightCalfCm; m.LeftAnkleCm = t.LeftAnkleCm; m.RightAnkleCm = t.RightAnkleCm;
    }

    public static FullBodyResponse Build(DateOnly date, BodyMetric? visual, BodyMetric? tape, decimal? scaleWeightKg = null, decimal? trendWeightKg = null)
    {
        var tapeModel = tape is null ? null : ToModel(tape);
        var hasTape = tapeModel is { HasAnyValue: true };
        return new FullBodyResponse(date, visual?.BodyFatPercent, hasTape ? tapeModel : null, visual?.BodyFatPercent is not null || hasTape, scaleWeightKg, trendWeightKg);
    }

    public static async Task<(BodyMetric? Visual, BodyMetric? Tape)> LoadAsync(IUnitOfWork uow, Guid userId, DateOnly date, bool tracking, CancellationToken ct)
    {
        var rows = await uow.Repository<BodyMetric>().Query(tracking)
            .Where(b => b.UserId == userId && b.MeasuredDate == date && (b.Method == MeasurementMethod.Visual || b.Method == MeasurementMethod.Tape))
            .OrderByDescending(b => b.CreatedAt)   // MỚI NHẤT thắng, cùng quy tắc đọc series/overview/history (trước đây lấy hàng CŨ NHẤT khi trùng ngày)
            .ToListAsync(ct);
        return (rows.FirstOrDefault(r => r.Method == MeasurementMethod.Visual), rows.FirstOrDefault(r => r.Method == MeasurementMethod.Tape));
    }
}
