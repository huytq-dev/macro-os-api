namespace Contract;

/// <summary>
/// Số đo vòng cơ thể (cm), mọi ô tuỳ chọn. Tên UI → trường: Bicep = <c>LeftArmCm/RightArmCm</c>, Hips = <c>HipCm</c>.
/// </summary>
public sealed record TapeMeasurementsModel(
    decimal? NeckCm,
    decimal? ShouldersCm,
    decimal? BustCm,
    decimal? ChestCm,
    decimal? WaistCm,
    decimal? HipCm,
    decimal? LeftArmCm,
    decimal? RightArmCm,
    decimal? LeftForearmCm,
    decimal? RightForearmCm,
    decimal? LeftWristCm,
    decimal? RightWristCm,
    decimal? LeftThighCm,
    decimal? RightThighCm,
    decimal? LeftCalfCm,
    decimal? RightCalfCm,
    decimal? LeftAnkleCm,
    decimal? RightAnkleCm
)
{
    /// <summary>Có ít nhất một ô được nhập (dùng để quyết định giữ hay xoá hàng Tape).</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public bool HasAnyValue => GetType().GetProperties().Any(p => p.PropertyType == typeof(decimal?) && p.GetValue(this) is not null);
}
