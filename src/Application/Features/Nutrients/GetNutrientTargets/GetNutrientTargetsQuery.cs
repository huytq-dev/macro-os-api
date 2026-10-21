namespace Application;

/// <summary>UC-D09: danh mục nutrient kèm target hiệu lực + pin của user, không có lượng đã ăn (màn đặt target/pin).</summary>
public sealed record GetNutrientTargetsQuery(bool All = false) : IRequest<IReadOnlyList<NutrientModel>>;
