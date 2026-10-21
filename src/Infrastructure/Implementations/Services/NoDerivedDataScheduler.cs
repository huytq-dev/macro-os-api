namespace Infrastructure;

/// <summary>Không có chạy nền: caller dựng dữ liệu dẫn xuất đồng bộ như trước (test, công cụ dòng lệnh).</summary>
internal sealed class NoDerivedDataScheduler : IDerivedDataScheduler
{
    public static readonly NoDerivedDataScheduler Instance = new();
    public bool IsAvailable => false;
    public void Schedule(Guid userId, DateOnly fromDate, IReadOnlyCollection<DateOnly>? nutritionDates) => throw new InvalidOperationException("Không có scheduler nền.");
}
