namespace Contract;

/// <summary>Giới hạn ghi chú của một ngày (FE hiển thị "n / 5" bằng <see cref="DayLogResponse.NotesMaxPerDay"/>).</summary>
public static class FoodLogNoteLimits
{
    public const int MaxPerDay = 5;
    public const int NameMaxLength = 60;
    public const int TextMaxLength = 1000;
}
