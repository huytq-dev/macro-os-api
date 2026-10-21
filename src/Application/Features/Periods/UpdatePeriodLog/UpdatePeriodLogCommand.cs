namespace Application;

/// <summary>UC-E05: sửa kỳ kinh (đổi ngày, đóng kỳ bằng EndDate, triệu chứng/ghi chú); tính lại độ dài chu kỳ cả chuỗi.</summary>
public sealed record UpdatePeriodLogCommand(
    [property: System.Text.Json.Serialization.JsonIgnore] Guid Id,
    DateOnly StartDate,
    DateOnly? EndDate,
    string? Symptoms,
    string? Notes
) : IRequest<Unit>;
