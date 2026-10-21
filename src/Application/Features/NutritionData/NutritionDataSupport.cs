namespace Application;

public static class NutritionDataSupport
{
    public const string AppSource = "App", ManualSource = "Manual";

    public static string SourceOf(bool fasting, int entryCount, int quickAddCount)
        => fasting || (entryCount > 0 && quickAddCount == entryCount) ? ManualSource : AppSource;

    /// <summary>Ngày có ít nhất một entry không phải Quick Add (Source = App): dữ liệu của Food Log, NDM không được ghi đè.</summary>
    public static bool IsAppDay(IEnumerable<FoodLogEntry> liveEntries) => liveEntries.Any(e => e.Source != EntrySource.QuickAdd);
}
