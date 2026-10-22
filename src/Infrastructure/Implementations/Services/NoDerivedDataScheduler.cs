namespace Infrastructure;

/// <summary>Không có worker nền (test, công cụ dòng lệnh): job vẫn được lưu trong DB và được xử lý khi có worker hoặc khi gọi tay.</summary>
internal sealed class NoDerivedDataScheduler : IDerivedDataScheduler
{
    public static readonly NoDerivedDataScheduler Instance = new();
    public void Wake() { }
    public void ReportSync(string cause, long elapsedMs, int rowsWritten, string outcome) { }
}
