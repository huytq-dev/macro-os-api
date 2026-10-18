using System.Globalization;
using System.Text;

namespace Application;

/// <summary>Chuẩn hoá tên thực phẩm để so khớp: chữ thường, bỏ dấu tiếng Việt (kể cả đ), dấu câu → khoảng trắng, gộp khoảng trắng. "Nước mắm" và "nuoc mam" cùng ra "nuoc mam".</summary>
public static class FoodNameNormalizer
{
    public static string Normalize(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "";
        var d = s.Trim().ToLowerInvariant().Replace('đ', 'd').Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(d.Length);
        var lastSpace = true;
        foreach (var c in d)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark) continue;
            if (char.IsLetterOrDigit(c)) { sb.Append(c); lastSpace = false; }
            else if (!lastSpace) { sb.Append(' '); lastSpace = true; }
        }
        return sb.ToString().TrimEnd();
    }

    /// <summary>Số ít đơn giản cho tiếng Anh ("eggs" → "egg"); tên nhiều từ chỉ đổi từ cuối. Từ bất quy tắc để alias lo.</summary>
    public static string Singular(string normalized)
    {
        var i = normalized.LastIndexOf(' ');
        var last = normalized[(i + 1)..];
        if (last.Length > 3 && last.EndsWith('s') && !last.EndsWith("ss")) last = last[..^1];
        return i < 0 ? last : normalized[..(i + 1)] + last;
    }

    /// <summary>vi-VN nếu tên còn dấu/đ tiếng Việt, ngược lại en-US (tên Việt không dấu tính là en-US; so khớp theo chuỗi chuẩn hoá nên vẫn khớp).</summary>
    public static string DetectLocale(string? s)
        => s is not null && s.Any(c => c is 'đ' or 'Đ' || (c > 127 && char.IsLetter(c) && c.ToString().Normalize(NormalizationForm.FormD).Length > 1)) ? "vi-VN" : "en-US";
}
