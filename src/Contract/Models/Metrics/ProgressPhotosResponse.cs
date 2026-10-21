namespace Contract;

/// <param name="HasConsent">Đã từng đồng ý lưu ảnh riêng tư; false thì lần tải đầu phải gửi consent = true.</param>
/// <param name="Photos">Thứ tự Front, Side, Back.</param>
/// <param name="ScaleWeightKg">Cân nặng của đúng ngày này nếu có.</param>
/// <param name="TrendWeightKg">Trend cân nặng của ngày này nếu tính được.</param>
public sealed record ProgressPhotosResponse(DateOnly Date, bool HasConsent, IReadOnlyList<ProgressPhotoModel> Photos, decimal? ScaleWeightKg = null, decimal? TrendWeightKg = null);
