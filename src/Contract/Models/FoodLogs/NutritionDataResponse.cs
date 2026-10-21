namespace Contract;

/// <param name="Items">Mới nhất trước; chỉ ngày có ít nhất một entry hoặc đánh dấu nhịn ăn.</param>
public sealed record NutritionDataResponse(IReadOnlyList<NutritionDataDayModel> Items);
