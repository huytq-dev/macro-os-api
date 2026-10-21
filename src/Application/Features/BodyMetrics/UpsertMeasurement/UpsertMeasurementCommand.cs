namespace Application;

/// <summary>Ghi MỘT số đo của một ngày (cm; Date null = hôm nay theo múi giờ user, không ở tương lai); ngày đã có giá trị thì ghi đè. Các số đo khác của ngày đó không đổi.</summary>
public sealed record UpsertMeasurementCommand(
    // Nullable: route gán Key sau khi bind body; non-nullable thì model validation báo "Key field is required" trước controller.
    [property: System.Text.Json.Serialization.JsonIgnore] string? Key,
    DateOnly? Date,
    decimal ValueCm) : IRequest<MeasurementEntryModel>;
