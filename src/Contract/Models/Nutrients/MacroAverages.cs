namespace Contract;

/// <summary>
/// Trung bình MỖI NGÀY ĐÃ LOG (ngày có số, không IsIncomplete; nhịn ăn tính là ngày log 0 kcal, như series từng nutrient) — KHÔNG chia cho số ngày lịch. Tất cả null khi không có ngày nào.
/// <c>*Percent</c> = trung bình kcal của phần đó ÷ trung bình kcal tổng × 100 (1 số lẻ); null khi trung bình kcal ≤ 0. Bốn phần có thể không cộng đúng 100 do làm tròn; Other âm làm tổng ≠ 100 một cách có chủ đích (số thật).
/// </summary>
public sealed record MacroAverages(
    decimal? CaloriesKcal,
    decimal? ProteinG,
    decimal? FatG,
    decimal? CarbsG,
    decimal? ProteinKcal,
    decimal? FatKcal,
    decimal? CarbsKcal,
    decimal? OtherKcal,
    decimal? ProteinPercent,
    decimal? FatPercent,
    decimal? CarbsPercent,
    decimal? OtherPercent
);
