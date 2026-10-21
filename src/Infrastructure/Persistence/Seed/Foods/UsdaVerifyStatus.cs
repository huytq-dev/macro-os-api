namespace Infrastructure;

public static class UsdaVerifyStatus
{
    /// <summary>Khớp tên chắc và kcal không lệch bất thường: được ghi khi --apply.</summary>
    public const string Match = "match";
    /// <summary>Khớp tên chắc nhưng kcal lệch &gt; 35 %: nghi khớp nhầm, KHÔNG ghi (chỉ định tay bằng --fdc nếu đúng).</summary>
    public const string Review = "review";
    /// <summary>Khớp tên yếu: chỉ in ứng viên gần nhất, không ghi.</summary>
    public const string Weak = "weak";
    /// <summary>Không có món USDA tương đương: món Việt giữ nguyên nguồn bảng VN (Unverified).</summary>
    public const string None = "none";
    public const string Error = "error";
    public const string NotChecked = "not-checked";
    public const string Already = "already";
}
