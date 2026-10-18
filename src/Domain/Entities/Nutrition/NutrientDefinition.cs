namespace Domain;

// Master danh mục chất dinh dưỡng; thêm nutrient = thêm row, không thêm cột
public class NutrientDefinition : EntityBase<Guid>
{
    public string Code { get; set; } = null!;       // energy_kcal, protein_g, sodium_mg...
    public string Name { get; set; } = null!;
    public string? NameVi { get; set; }
    public string Unit { get; set; } = null!;       // g, mg, mcg, kcal
    public NutrientGroup Group { get; set; }
    public int DisplayOrder { get; set; }
    public byte DisplayPrecision { get; set; }
    public decimal? ReferenceMin { get; set; }      // floor/ngày; mặc định toàn cục, user ghi đè ở UserNutrientTarget
    public decimal? ReferenceTarget { get; set; }   // target/RDA
    public decimal? ReferenceMax { get; set; }      // ceiling
    public string? UsdaNumber { get; set; }         // số chất của USDA FoodData Central (vd 203 = Protein); null nếu chất tính toán / không có ở USDA
    public bool IsCore { get; set; } = true;        // chất hiển thị mặc định (bộ 56 khoá của FE); false = chất chi tiết (acid béo, đường, tocopherol…), chỉ trả khi ?all=true
}
