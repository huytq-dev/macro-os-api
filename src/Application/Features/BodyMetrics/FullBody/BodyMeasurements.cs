namespace Application;

public static class BodyMeasurements
{
    /// <summary>Khoá của % mỡ Visual trong các thao tác theo khoá (xoá/so sánh); không phải số đo vòng.</summary>
    public const string VisualBodyFatKey = "visualBodyFat";

    public const decimal VisualBodyFatMin = 2m;
    public const decimal VisualBodyFatMax = 70m;

    public const string UpperBody = "UpperBody", Arms = "Arms", Legs = "Legs";

    // Khoảng hợp lý (cm) để chặn nhập nhầm đơn vị/chữ số; không phải giới hạn sinh lý
    public static readonly IReadOnlyList<BodyMeasurement> All =
    [
        new("neck", UpperBody, nameof(TapeMeasurementsModel.NeckCm), 20, 70, m => m.NeckCm, (m, v) => m.NeckCm = v, t => t.NeckCm),
        new("shoulders", UpperBody, nameof(TapeMeasurementsModel.ShouldersCm), 60, 200, m => m.ShouldersCm, (m, v) => m.ShouldersCm = v, t => t.ShouldersCm),
        new("bust", UpperBody, nameof(TapeMeasurementsModel.BustCm), 50, 200, m => m.BustCm, (m, v) => m.BustCm = v, t => t.BustCm),
        new("chest", UpperBody, nameof(TapeMeasurementsModel.ChestCm), 50, 200, m => m.ChestCm, (m, v) => m.ChestCm = v, t => t.ChestCm),
        new("waist", UpperBody, nameof(TapeMeasurementsModel.WaistCm), 40, 250, m => m.WaistCm, (m, v) => m.WaistCm = v, t => t.WaistCm),
        new("hips", UpperBody, nameof(TapeMeasurementsModel.HipCm), 50, 250, m => m.HipCm, (m, v) => m.HipCm = v, t => t.HipCm),
        new("leftBicep", Arms, nameof(TapeMeasurementsModel.LeftArmCm), 10, 80, m => m.LeftArmCm, (m, v) => m.LeftArmCm = v, t => t.LeftArmCm),
        new("rightBicep", Arms, nameof(TapeMeasurementsModel.RightArmCm), 10, 80, m => m.RightArmCm, (m, v) => m.RightArmCm = v, t => t.RightArmCm),
        new("leftForearm", Arms, nameof(TapeMeasurementsModel.LeftForearmCm), 10, 60, m => m.LeftForearmCm, (m, v) => m.LeftForearmCm = v, t => t.LeftForearmCm),
        new("rightForearm", Arms, nameof(TapeMeasurementsModel.RightForearmCm), 10, 60, m => m.RightForearmCm, (m, v) => m.RightForearmCm = v, t => t.RightForearmCm),
        new("leftWrist", Arms, nameof(TapeMeasurementsModel.LeftWristCm), 8, 35, m => m.LeftWristCm, (m, v) => m.LeftWristCm = v, t => t.LeftWristCm),
        new("rightWrist", Arms, nameof(TapeMeasurementsModel.RightWristCm), 8, 35, m => m.RightWristCm, (m, v) => m.RightWristCm = v, t => t.RightWristCm),
        new("leftThigh", Legs, nameof(TapeMeasurementsModel.LeftThighCm), 20, 120, m => m.LeftThighCm, (m, v) => m.LeftThighCm = v, t => t.LeftThighCm),
        new("rightThigh", Legs, nameof(TapeMeasurementsModel.RightThighCm), 20, 120, m => m.RightThighCm, (m, v) => m.RightThighCm = v, t => t.RightThighCm),
        new("leftCalf", Legs, nameof(TapeMeasurementsModel.LeftCalfCm), 15, 80, m => m.LeftCalfCm, (m, v) => m.LeftCalfCm = v, t => t.LeftCalfCm),
        new("rightCalf", Legs, nameof(TapeMeasurementsModel.RightCalfCm), 15, 80, m => m.RightCalfCm, (m, v) => m.RightCalfCm = v, t => t.RightCalfCm),
        new("leftAnkle", Legs, nameof(TapeMeasurementsModel.LeftAnkleCm), 10, 45, m => m.LeftAnkleCm, (m, v) => m.LeftAnkleCm = v, t => t.LeftAnkleCm),
        new("rightAnkle", Legs, nameof(TapeMeasurementsModel.RightAnkleCm), 10, 45, m => m.RightAnkleCm, (m, v) => m.RightAnkleCm = v, t => t.RightAnkleCm),
    ];

    public static BodyMeasurement? Find(string? key) => All.FirstOrDefault(m => string.Equals(m.Key, key, StringComparison.OrdinalIgnoreCase));

    public static bool IsKnownKey(string? key) => Find(key) is not null || string.Equals(key, VisualBodyFatKey, StringComparison.OrdinalIgnoreCase);

    /// <summary>Số ô có giá trị của một hàng Tape.</summary>
    public static int CountValues(BodyMetric? tape) => tape is null ? 0 : All.Count(m => m.Get(tape) is not null);

    public static bool HasAny(BodyMetric tape) => All.Any(m => m.Get(tape) is not null);
}
