namespace Application;

/// <summary>
/// Mở rộng từ khoá tìm món CHỈ ở mức hình thái từ (số nhiều → số ít: "eggs" → "egg", "whites" → "white"). KHÔNG dịch Việt↔Anh và KHÔNG coi các cách chế biến là một (boiled ≠ cooked ≠ fried ≠ raw):
/// quyết định user 2026-10-26 "Không tự dịch tên thực phẩm bằng mapping thiếu kiểm chứng; tách raw/cooked/boiled/fried". Tìm tiếng Việt/đồng nghĩa đi qua alias đã duyệt trong catalog (FoodAlias), không qua bảng từ trong code.
/// Mỗi từ khoá thành NHÓM lựa chọn (chính nó + số ít); món khớp khi MỖI nhóm có ít nhất một lựa chọn nằm trong tên. Chỉ MỞ RỘNG khớp, không loại món đang khớp. Đầu vào đã gấp bỏ dấu (<see cref="FoodCatalogIndex.Fold"/>).
/// </summary>
public static class FoodSearchSynonyms
{
    /// <summary>Tách từ khoá đã gấp thành các nhóm lựa chọn (từ gốc trước, rồi số ít của danh từ số nhiều).</summary>
    public static string[][] Groups(string folded)
        => folded.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries).Distinct().Select(Expand).ToArray();

    private static string[] Expand(string token)
        // Chỉ từ ≥ 4 ký tự, bỏ "ss" như "glass"; "es" để mặc: khớp chuỗi con đã phủ phần lớn
        => token.Length >= 4 && token[^1] == 's' && token[^2] != 's' ? [token, token[..^1]] : [token];
}
