namespace Contract;

/// <param name="Items">Mới nhất trước; chỉ ngày có ít nhất một entry, manual override hoặc đánh dấu nhịn ăn.</param>
/// <param name="HasMore">Còn ngày cũ hơn trang này (theo from/to/limit đã gửi).</param>
/// <param name="NextBefore">Ngày cuối (cũ nhất) của trang này: gửi lại làm <c>before</c> để lấy trang kế; null khi hết.</param>
public sealed record NutritionDataResponse(IReadOnlyList<NutritionDataDayModel> Items, bool HasMore = false, DateOnly? NextBefore = null);
