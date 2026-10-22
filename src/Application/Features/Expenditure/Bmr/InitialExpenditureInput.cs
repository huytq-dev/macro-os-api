namespace Application;

/// <summary>
/// Đầu vào của bộ tính BMR / TDEE khởi điểm DÙNG CHUNG (onboarding, Preview, Create, Expenditure Settings). Mọi trường null = CHƯA có câu trả lời xác nhận: bộ tính không điền mặc định.
/// <paramref name="BmrSex"/> là tham số sinh học user xác nhận (tách khỏi <paramref name="Gender"/>); Gender = Other không bao giờ được ánh xạ sang nam/nữ.
/// <paramref name="AdaptationFactor"/>: hệ số thích nghi đã giải quyết (mặc định 1 = không áp; điều kiện Prior Weight/Progress chưa xác minh nên chưa nối).
/// </summary>
public sealed record InitialExpenditureInput(
    decimal WeightKg,
    DateOnly? DateOfBirth,
    decimal? HeightCm,
    Gender? Gender,
    BiologicalSex? BmrSex,
    BodyFatBand? BodyFat,
    DailyActivityBand? DailyActivity,
    ExerciseSessionsBand? Exercise,
    DateOnly LocalToday,
    decimal AdaptationFactor = 1m);
