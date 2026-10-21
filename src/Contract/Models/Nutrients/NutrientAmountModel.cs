namespace Contract;

/// <summary>Một chất trong màn chi tiết món/entry.</summary>
/// <param name="Amount">Lượng theo khẩu phần/entry; null = chưa có dữ liệu (khác 0).</param>
/// <param name="Floor">Mức hiệu lực của ngày hỏi (user ghi đè, thiếu thì mặc định; macro lấy từ program).</param>
/// <param name="PercentOfTarget">Amount / Target × 100 (làm tròn 1 số lẻ); null khi thiếu Amount hoặc Target.</param>
/// <param name="IsCore">false = chất chi tiết của USDA (acid béo, đường…); chỉ có trong danh sách khi món có dữ liệu chất đó.</param>
public sealed record NutrientAmountModel(
    string Code,
    string Name,
    string Unit,
    string Group,
    int DisplayOrder,
    int DisplayPrecision,
    decimal? Amount,
    decimal? Floor,
    decimal? Target,
    decimal? Ceiling,
    decimal? PercentOfTarget,
    bool IsCore,
    string? Key = null
);
