namespace Application;

/// <summary>
/// Quy tắc ngày dùng chung cho validator, MỘT nơi đọc đồng hồ (<see cref="TimeProvider"/>) thay vì rải <c>DateTime.UtcNow</c> khắp các validator.
/// Validator chỉ chặn thô theo UTC (dung sai tới "ngày mai UTC" cho múi giờ UTC+14); chặn chính xác theo múi giờ user ở handler (<c>UserLocale.EnsureNotFuture</c>).
/// </summary>
public static class DateRuleBuilderExtensions
{
    /// <summary>Hôm nay theo UTC.</summary>
    public static DateOnly UtcToday => DateOnly.FromDateTime(TimeProvider.System.GetUtcNow().UtcDateTime);

    /// <summary>Ngày không sau "ngày mai UTC" (400 <see cref="ValidationMessages.NotInFuture"/>).</summary>
    public static IRuleBuilderOptions<T, DateOnly> NotInFuture<T>(this IRuleBuilder<T, DateOnly> rule)
        => rule.LessThanOrEqualTo(_ => UtcToday.AddDays(1)).WithMessage(ValidationMessages.NotInFuture);

    /// <summary>Như trên cho ngày tuỳ chọn (null bỏ qua quy tắc này).</summary>
    public static IRuleBuilderOptions<T, DateOnly?> NotInFuture<T>(this IRuleBuilder<T, DateOnly?> rule)
        => rule.LessThanOrEqualTo(_ => UtcToday.AddDays(1)).WithMessage(ValidationMessages.NotInFuture);
}
