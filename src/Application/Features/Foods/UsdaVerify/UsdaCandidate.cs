using System.Text.RegularExpressions;

namespace Application;

/// <param name="CoverageOurs">Tỉ lệ từ khoá của tên món mình có mặt trong mô tả FDC.</param>
/// <param name="CoverageUsda">Tỉ lệ từ của mô tả FDC được tên món mình phủ (mô tả càng dài càng thấp).</param>
/// <param name="InformativeHit">Có ít nhất một từ KHÔNG phải từ chế biến/hình thức chung (ground, raw, cooked…) trùng với FDC; chặn "Cinnamon, Ground" khớp "Beef, ground".</param>
/// <param name="Mismatch">Lý do loại ứng viên dù trùng nhiều từ (món thương hiệu/trẻ em, từ thừa đổi bản chất như oil/juice/yogurt, bổ ngữ lạ trong cụm danh từ chính…); null = không bị loại.</param>
public sealed record UsdaCandidate(UsdaFood Food, decimal CoverageOurs, decimal CoverageUsda, decimal Score, bool InformativeHit = true, string? Mismatch = null)
{
    public UsdaMatchGrade Grade => Mismatch is not null ? UsdaMatchGrade.None : UsdaCatalogMatcher.Grade(CoverageOurs, Score, InformativeHit);
}
