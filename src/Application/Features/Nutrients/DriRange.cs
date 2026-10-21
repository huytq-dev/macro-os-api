namespace Application;

/// <param name="Floor">Mức tối thiểu thật sự (LTI) — chỉ đặt khi có số liệu thật; hiện chưa chất nào có nên luôn null (RDA/AI là Target, không phải Floor).</param>
/// <param name="Target">Mục tiêu (RDA/AI); null nếu chất chỉ có trần.</param>
/// <param name="Ceiling">Trần (UL chỉ khi UL áp cho tổng lượng ăn từ thực phẩm; trần khuyến nghị DGA/FDA/CDRR); null nếu không có trần.</param>
public readonly record struct DriRange(decimal? Floor, decimal? Target, decimal? Ceiling)
{
    public static readonly DriRange None = new(null, null, null);
    public bool IsNone => Floor is null && Target is null && Ceiling is null;
}
