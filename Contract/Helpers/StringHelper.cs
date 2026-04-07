using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Contract;

public static class StringHelper
{
    /// <summary>
    /// Bỏ dấu tiếng Việt. Dùng để normalize search text.
    /// Ví dụ: "Nguyễn Văn A" → "Nguyen Van A"
    /// </summary>
    public static string RemoveDiacritics(string input)
    {
        var normalized = input.Normalize(NormalizationForm.FormD);
        var chars = normalized.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark);
        return new string(chars.ToArray()).Normalize(NormalizationForm.FormC);
    }

    /// <summary>
    /// Sinh mã ngẫu nhiên gồm chữ in hoa và số.
    /// Ví dụ: GenerateCode(8) → "A3KF9XBD"
    /// </summary>
    public static string GenerateCode(int length)
    {
        if (length <= 0) return string.Empty;

        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        var guidPart = Guid.NewGuid().ToString("N").ToUpperInvariant();
        var randomPart = new string(Enumerable.Range(0, length)
            .Select(_ => chars[random.Next(chars.Length)]).ToArray());

        var combined = guidPart + randomPart;
        return combined[..Math.Min(length, combined.Length)];
    }

    /// <summary>
    /// Bỏ dấu nháy bao ngoài chuỗi nếu có.
    /// Ví dụ: "\"hello\"" → "hello"
    /// </summary>
    public static string Unwrap(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return input;

        if (input.StartsWith("\"") && input.EndsWith("\""))
            return input[1..^1].Replace("\\\"", "\"");

        return input;
    }
}
