namespace Application;

/// <summary>
/// Quy tắc dùng chung của Manual Nutrition (S29): tổng dinh dưỡng CỦA NGÀY nằm ở 4 cột nullable của FoodLogs, TÁCH khỏi FoodLogEntries (Quick Add vẫn là một entry bình thường).
/// Không hàm nào ở đây xoá/sửa entry, ghi chú, nước; mọi thay đổi chỉ chạm các cột/cờ của đúng ngày đó, trong cùng một SaveChanges của handler gọi.
/// </summary>
public static class NutritionDataSupport
{
    public const string ReasonFastingWithManual = "FASTING_WITH_MANUAL_NUTRITION", ReasonFastingNeedsEmptyDay = "FASTING_NEEDS_EMPTY_DAY",
        ReasonNothingToSave = "NOTHING_TO_SAVE", ReasonUseRemoveOverride = "USE_REMOVE_OVERRIDE";

    private static IDictionary<string, string[]> Reason(string code) => new Dictionary<string, string[]> { ["reasonCode"] = [code] };

    /// <summary>Ngày có override: ít nhất một trường manual khác null (calories-only, macro-only, manual 0 đều tính).</summary>
    public static bool HasManual(FoodLog l) => l.ManualCaloriesKcal is not null || l.ManualProteinG is not null || l.ManualCarbsG is not null || l.ManualFatG is not null;

    /// <summary>Xoá đúng 4 cột manual (Remove Override). Trả true nếu có gì thay đổi.</summary>
    public static bool ClearManual(FoodLog l)
    {
        var had = HasManual(l);
        (l.ManualCaloriesKcal, l.ManualProteinG, l.ManualCarbsG, l.ManualFatG) = (null, null, null, null);
        return had;
    }

    /// <summary>Manual dương theo bất kỳ trường nào (calories hay macro): xung đột với ngày nhịn ăn.</summary>
    public static bool ManualPositive(decimal? kcal, decimal? protein, decimal? carbs, decimal? fat) => kcal > 0 || protein > 0 || carbs > 0 || fat > 0;

    public static BadRequestException FastingWithManual() => new(ExceptionMessages.FastingWithManualNutrition, Reason(ReasonFastingWithManual));

    public static ConflictException FastingBlocked() => new(ExceptionMessages.FastingNeedsEmptyDay, Reason(ReasonFastingNeedsEmptyDay));

    public static BadRequestException NothingToSave() => new(ExceptionMessages.NutritionDataNothingToSave, Reason(ReasonNothingToSave));

    public static BadRequestException UseRemoveOverride() => new(ExceptionMessages.NutritionDataUseRemoveOverride, Reason(ReasonUseRemoveOverride));

    /// <summary>
    /// Ngày TRỐNG THẬT (R2-4): không còn entry nào, không còn manual ở bất kỳ trường nào (calories-only, macro-only, manual 0), không phải ngày nhịn ăn (nhịn ăn là một nguồn hợp lệ).
    /// Không suy ra từ TotalCalories = 0 (entry 0 kcal vẫn là entry; manual 0 vẫn là manual). Ghi chú và nước không làm ngày "có dữ liệu dinh dưỡng".
    /// </summary>
    public static bool IsEmptyDay(FoodLog log, IReadOnlyCollection<FoodLogEntry> liveEntries) => liveEntries.Count == 0 && !HasManual(log) && !log.IsFasting;

    /// <summary>
    /// R2-4: ngày vừa thành trống thật thì tự bỏ cờ chưa-đủ (cờ cũ không được âm thầm loại ngày được log lại sau đó khỏi Expenditure). CHỈ đổi IsIncomplete, cùng SaveChanges với thao tác làm ngày trống
    /// (nên thao tác lỗi thì cờ không đổi; RowVersion của FoodLog bắt đua với thêm món). Trả true nếu đã đổi cờ.
    /// </summary>
    public static bool ClearStaleIncomplete(FoodLog log, IReadOnlyCollection<FoodLogEntry> liveEntries)
    {
        if (!log.IsIncomplete || !IsEmptyDay(log, liveEntries)) return false;
        log.IsIncomplete = false;
        return true;
    }

    public static List<FoodLogEntry> Live(FoodLog log) => log.Entries.Where(e => !e.IsDeleted).ToList();

    /// <summary>Ngày NDM (Contract v2) từ FoodLog đang nạp cùng số entry chưa xoá; cùng một phép giải nguồn với mọi nơi đọc ngày (<see cref="FoodLogSupport.LoggedDay"/>).</summary>
    public static NutritionDataDayModel ToModel(FoodLog log, IReadOnlyCollection<FoodLogEntry> live)
        => ToModel(FoodLogSupport.LoggedDay.From(log.LogDate, log.TotalCalories, log.TotalProtein, log.TotalCarbs, log.TotalFat, log.TotalFiber, log.TotalWater, log.IsIncomplete, log.IsFasting, live.Count > 0,
            log.ManualCaloriesKcal, log.ManualProteinG, log.ManualCarbsG, log.ManualFatG), live.Count);

    public static NutritionDataDayModel ToModel(FoodLogSupport.LoggedDay d, int entryCount)
        => new(d.Date, d.Calories, d.Protein, d.Fat, d.Carbs, d.Source, d.IsFasting, d.IsIncomplete, entryCount, d.HasOverride,
            d.ManualCalories, d.ManualProtein, d.ManualFat, d.ManualCarbs, d.FoodCalories, d.FoodProtein, d.FoodFat, d.FoodCarbs, d.IsValidForAlgorithms, d.Conflict);

    /// <summary>Xoá mềm entry (interceptor) và đánh dấu ngay trong unit of work để RecalcTotals bỏ qua; KHÔNG bao giờ xoá dòng FoodLog.</summary>
    public static async Task SoftDeleteAsync(IUnitOfWork uow, IEnumerable<FoodLogEntry> entries, CancellationToken ct)
    {
        foreach (var e in entries.ToList())
        {
            await uow.Repository<FoodLogEntry>().RemoveAsync(e, ct);
            e.IsDeleted = true;
        }
    }

    /// <summary>
    /// Đặt override của ngày bằng ĐÚNG các giá trị đã gửi (null = chưa nhập, 0 = nhập 0; không điền từ Food Log, không suy calories từ macro). Có thể kèm đổi cờ (null = giữ).
    /// Ném (không đổi gì): nhịn ăn hiệu lực + manual dương (400 FASTING_WITH_MANUAL_NUTRITION); bật nhịn ăn trên ngày đã có entry (409). Trả true nếu có trường nào đổi.
    /// </summary>
    public static bool ApplyManual(FoodLog log, IReadOnlyCollection<FoodLogEntry> live, decimal? kcal, decimal? protein, decimal? carbs, decimal? fat, bool? fasting, bool? incomplete)
    {
        var finalFasting = fasting ?? log.IsFasting;
        if (fasting == true && live.Count > 0) throw FastingBlocked();
        if (finalFasting && live.Count == 0 && ManualPositive(kcal, protein, carbs, fat)) throw FastingWithManual();

        var before = (log.ManualCaloriesKcal, log.ManualProteinG, log.ManualCarbsG, log.ManualFatG, log.IsFasting, log.IsIncomplete);
        (log.ManualCaloriesKcal, log.ManualProteinG, log.ManualCarbsG, log.ManualFatG) = (kcal, protein, carbs, fat);
        if (fasting is { } f) log.IsFasting = f && live.Count == 0;
        if (incomplete is { } i) log.IsIncomplete = i;
        return before != (log.ManualCaloriesKcal, log.ManualProteinG, log.ManualCarbsG, log.ManualFatG, log.IsFasting, log.IsIncomplete);
    }
}
