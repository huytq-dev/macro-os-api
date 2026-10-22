namespace Application;

/// <summary>Nguồn của TDEE khởi điểm, theo thứ tự ưu tiên của <see cref="InitialTdeeResolver"/>.</summary>
public static class TdeeSources
{
    /// <summary>Expenditure động đang Updating: TDEE đã học hiện tại.</summary>
    public const string LearnedUpdating = "LearnedUpdating";

    /// <summary>Đã từng học (có snapshot Updating) nhưng hiện Holding hoặc vừa reset: giá trị expenditure hợp lệ gần nhất, KHÔNG quay về công thức.</summary>
    public const string LearnedHolding = "LearnedHolding";

    /// <summary>Chưa có ước tính đã học: TDEE khởi điểm user tự nhập (khác Static Mode, không ghi đè TDEE đã học).</summary>
    public const string ManualInitial = "ManualInitial";

    /// <summary>Chưa có gì khác: công thức theo hồ sơ (Cunningham khi có % mỡ, ngược lại Mifflin-St Jeor).</summary>
    public const string Formula = "Formula";
}
