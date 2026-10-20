namespace Contract;

/// <param name="Group">Energy, Macro, Carb, Fat, AminoAcid, Vitamin, Mineral, Other — quyết định section và màu ở FE.</param>
/// <param name="Amount">Lượng đã ăn trong ngày; null = chưa có dữ liệu nutrient (khác 0). Luôn null ở /nutrients/targets.</param>
/// <param name="Floor">Giá trị hiệu lực: dòng user ghi đè, thiếu thì mặc định của NutrientDefinition; Calories/P/F/C lấy từ program (chỉ Target).</param>
/// <param name="IsCustomTarget">User đã ghi đè ít nhất một trong Floor/Target/Ceiling.</param>
/// <param name="PinOrder">1–8 nếu được pin lên Dashboard; null nếu không.</param>
/// <param name="IsCore">true = bộ chất hiển thị mặc định (56 khoá FE); false = chất chi tiết, chỉ có khi gọi với ?all=true.</param>
/// <param name="NoTarget">true = chất không có mức nào (Floor/Target/Ceiling đều null: EPA, DHA, amino acid, trans, Net carbs…): FE không vẽ vùng mục tiêu và không hiện % target.</param>
/// <param name="TargetBasis">Nguồn mức hiệu lực: user (ghi đè), program (Calories/P/C/F từ chương trình), dri (DRI theo giới + tuổi của hồ sơ), reference (DRI người lớn khi thiếu ngày sinh/giới: Target = mức cao nhất, Ceiling = mức thấp nhất giữa các nhóm), default (mức seed của chất), none.</param>
public sealed record NutrientModel(
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
    bool IsCustomTarget,
    int? PinOrder,
    bool IsCore,
    bool NoTarget,
    string TargetBasis
);

/// <summary>Mọi nutrient của một ngày; thêm <c>carbs_net_g</c> (Net non-fiber = carbs − fiber, tính khi đọc, không có target).</summary>
public sealed record DayNutrientsResponse(DateOnly Date, IReadOnlyList<NutrientModel> Items);
