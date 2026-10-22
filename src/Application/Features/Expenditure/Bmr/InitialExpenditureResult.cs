namespace Application;

/// <param name="TdeeKcal">null khi thiếu đầu vào (xem <paramref name="Missing"/>); không bao giờ điền giá trị mặc định.</param>
/// <param name="Method">MifflinStJeor | Cunningham | "" khi chưa tính được. (Chọn phương trình MF Eq1/Eq2/Eq3 chưa nối: chờ chính sách chọn được duyệt.)</param>
/// <param name="Missing">Tên các đầu vào còn thiếu (WeightKg, DateOfBirth, HeightCm, Sex, DailyActivity, ExerciseSessions).</param>
public sealed record InitialExpenditureResult(
    int? TdeeKcal, int? BmrKcal, string Method, decimal? BodyFatPercent, decimal ActivityMultiplier, IReadOnlyList<string> Missing)
{
    public bool IsComputable => TdeeKcal is not null;
}
