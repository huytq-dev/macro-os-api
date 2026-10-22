namespace Application;

/// <summary>Khẩu phần được chọn lúc sửa (tên + gram HIỆN TẠI của khẩu phần đó: đây là lựa chọn mới của user, không phải lịch sử).</summary>
public sealed record ServingRef(Guid Id, string Name, decimal Grams);

/// <param name="Unchanged">Số lượng và khẩu phần không đổi: chỉ bữa/giờ đổi, dinh dưỡng giữ nguyên tuyệt đối (không đọc lại món).</param>
/// <param name="Ratio">Hệ số co giãn các chất vi lượng đã lưu (FoodLogEntryNutrient) so với giá trị hiện tại.</param>
/// <param name="SnapshotJson">Snapshot gốc đã cập nhật khẩu phần hiện tại; null khi entry cũ không có snapshot gốc (giữ null, không bịa).</param>
public sealed record EntryEditPlan(bool Unchanged, decimal Ratio, EntryMacros? Macros, string? SnapshotJson);

/// <summary>
/// Sửa số lượng/khẩu phần của entry (hàm thuần, không DB): dinh dưỡng luôn dẫn xuất từ snapshot ĐÃ LƯU của entry, không từ FoodItem/Recipe hiện tại (BR-05, MacroFactor: sửa món/recipe không đổi lịch sử).
/// <list type="bullet">
/// <item>Cùng khẩu phần, chỉ đổi số lượng: tỉ lệ số lượng = tỉ lệ gram, không cần gram của khẩu phần. Có snapshot gốc ⇒ co giãn từ macro gốc (một lần làm tròn, không trôi); entry cũ ⇒ co giãn từ macro đang lưu.</item>
/// <item>Đổi khẩu phần: CHỈ khi có snapshot gốc hợp lệ (gram khẩu phần lúc log) + gram khẩu phần mới user vừa chọn. Entry cũ không có snapshot ⇒ TỪ CHỐI 400 LEGACY_SERVING_SNAPSHOT_MISSING, không dùng gram HIỆN TẠI của khẩu phần cũ, không đoán, không ghi gì.</item>
/// </list>
/// Chất vắng (null) vẫn null; không bao giờ biến thành 0.
/// </summary>
public static class EntryEdit
{
    public static EntryEditPlan Plan(FoodLogEntry e, decimal newQuantity, Guid? newServingId, ServingRef? newServing)
    {
        var same = newServingId == e.FoodServingId;
        if (same && newQuantity == e.Quantity) return new EntryEditPlan(true, 1m, null, e.ServingSnapshotJson);

        var snap = EntryServingSnapshot.Parse(e.ServingSnapshotJson);
        var isRecipe = e.RecipeId is not null;
        // Đổi khẩu phần cần snapshot lịch sử đầy đủ: khẩu phần cũ phải có gram lúc log, khẩu phần mới phải được chọn kèm gram
        if (!same && (snap is null || (e.FoodServingId is not null && snap.ServingGrams is null) || (newServingId is not null && newServing is null)))
            throw new BadRequestException(ValidationMessages.EntryServingUnverifiable,
                new Dictionary<string, string[]> { ["foodServingId"] = [EntryEditRefusalResponse.LegacyServingSnapshotMissing] },
                new EntryEditRefusalResponse(EntryEditRefusalResponse.LegacyServingSnapshotMissing, e.Id));

        // Gram một khẩu phần LÚC LOG; null = log theo gram (hoặc recipe)
        var oldWeight = isRecipe || e.FoodServingId is null ? null : snap?.ServingGrams;
        var newWeight = same ? oldWeight : newServing?.Grams;
        var oldKnown = isRecipe || e.FoodServingId is null || oldWeight is not null;
        var newKnown = isRecipe || newServingId is null || newWeight is not null;
        decimal? oldAmount = !oldKnown ? null : isRecipe ? e.Quantity : NutritionCalculator.Grams(e.Quantity, oldWeight);
        decimal? newAmount = !newKnown ? null : isRecipe ? newQuantity : NutritionCalculator.Grams(newQuantity, newWeight);

        decimal ratio;
        if (same) ratio = e.Quantity > 0 ? newQuantity / e.Quantity : throw new BadRequestException(ValidationMessages.EntryServingUnverifiable);
        else ratio = oldAmount is > 0 && newAmount is { } na0 ? na0 / oldAmount.Value : throw new BadRequestException(ValidationMessages.EntryServingUnverifiable);

        EntryMacros macros;
        if (snap is { BaseAmount: > 0 } && newAmount is { } na)
        {
            var r = na / snap.BaseAmount;
            macros = new EntryMacros(NutritionCalculator.Rescale(snap.Kcal, r), NutritionCalculator.Rescale(snap.Protein, r), NutritionCalculator.Rescale(snap.Carbs, r),
                NutritionCalculator.Rescale(snap.Fat, r), NutritionCalculator.Rescale(snap.Fiber, r), NutritionCalculator.Rescale(snap.Sodium, r));
        }
        else
            macros = new EntryMacros(NutritionCalculator.Rescale(e.Calories, ratio), NutritionCalculator.Rescale(e.Protein, ratio), NutritionCalculator.Rescale(e.Carbs, ratio),
                NutritionCalculator.Rescale(e.Fat, ratio), NutritionCalculator.Rescale(e.Fiber, ratio), NutritionCalculator.Rescale(e.Sodium, ratio));

        var json = snap is null ? null : (same ? snap : snap with { ServingId = newServingId, ServingName = newServing?.Name, ServingGrams = newServing?.Grams }).ToJson();
        return new EntryEditPlan(false, ratio, macros, json);
    }
}
