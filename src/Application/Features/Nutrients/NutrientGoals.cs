namespace Application;

/// <summary>
/// MỘT nơi quyết định mục tiêu hiệu lực của chất (thuần, không DB) và phân loại nó cho UI (<c>goalKind</c>).
/// Thứ tự: seed (NutrientDefinition.Reference*) → DRI CHỈ ở các trường DRI có định nghĩa → user (nguyên bộ). Calories/P/F/C do program đặt riêng (Target theo ngày).
/// </summary>
public static class NutrientGoals
{
    /// <summary>Có cả floor và ceiling: vẽ dải Range (target nếu có là vạch trong dải).</summary>
    public const string Range = "Range";
    /// <summary>Chỉ cần đạt mức tối thiểu: có floor (không ceiling), hoặc chỉ có target dạng RDA/AI (DRI, mặc định) — không có trần.</summary>
    public const string Minimum = "Minimum";
    /// <summary>Chỉ có trần (đường thêm, béo bão hoà, trans, caffeine).</summary>
    public const string Maximum = "Maximum";
    /// <summary>Một mức duy nhất do chương trình hoặc user đặt (Calories/P/F/C; user đặt riêng một target).</summary>
    public const string Target = "Target";
    /// <summary>Target kèm trần, không floor (natri 1500/2300, vitamin C 90/2000).</summary>
    public const string TargetWithCeiling = "TargetWithCeiling";
    public const string None = "None";

    /// <summary>Loại mục tiêu suy từ giá trị HIỆU LỰC (không lưu cột nào).</summary>
    public static string KindOf(decimal? floor, decimal? target, decimal? ceiling, string basis)
    {
        if (floor is not null && ceiling is not null) return Range;
        if (ceiling is not null) return target is not null ? TargetWithCeiling : Maximum;
        if (floor is not null) return Minimum;
        if (target is null) return None;
        return basis is NutrientCatalog.Basis.Program or NutrientCatalog.Basis.User ? Target : Minimum;
    }

    public static NutrientGoal Make(decimal? floor, decimal? target, decimal? ceiling, string basis)
        => new(floor, target, ceiling, basis, KindOf(floor, target, ceiling, basis));

    /// <summary>Floor ≤ Target ≤ Ceiling với các giá trị có mặt.</summary>
    public static bool IsOrdered(decimal? floor, decimal? target, decimal? ceiling)
        => (floor is null || target is null || floor <= target) && (target is null || ceiling is null || target <= ceiling) && (floor is null || ceiling is null || floor <= ceiling);

    /// <param name="dri">null = chất không thuộc bảng DRI hoặc chưa đủ dữ liệu (kcal); <see cref="DriRange.None"/> = DRI không đặt mức nào → seed được giữ.</param>
    /// <param name="driIsReference">DRI lấy mức tham chiếu người lớn vì thiếu hồ sơ.</param>
    public static NutrientGoal Resolve(decimal? seedFloor, decimal? seedTarget, decimal? seedCeiling, DriRange? dri, bool driIsReference, UserGoal? user)
    {
        if (user is { IsEmpty: false } u) return Make(u.Floor, u.Target, u.Ceiling, NutrientCatalog.Basis.User);

        var (floor, target, ceiling) = (seedFloor, seedTarget, seedCeiling);
        var basis = NutrientCatalog.Basis.Default;
        if (dri is { IsNone: false } d)
        {
            // Seed chỉ bổ sung trường DRI không định nghĩa; nếu trộn làm sai thứ tự thì lấy riêng DRI
            var merged = (Floor: d.Floor ?? floor, Target: d.Target ?? target, Ceiling: d.Ceiling ?? ceiling);
            (floor, target, ceiling) = IsOrdered(merged.Floor, merged.Target, merged.Ceiling) ? merged : (d.Floor, d.Target, d.Ceiling);
            basis = driIsReference ? NutrientCatalog.Basis.Reference : NutrientCatalog.Basis.Dri;
        }
        return Make(floor, target, ceiling, basis);
    }
}
