namespace Application;

/// <summary>Xoá MỘT số đo của một ngày (các số đo khác của ngày đó giữ nguyên); không có giá trị → 404.</summary>
public sealed record DeleteMeasurementCommand(string Key, DateOnly Date) : IRequest<Unit>;
