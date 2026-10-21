namespace Infrastructure;

/// <summary>
/// Báo cáo của verify-usda dưới dạng dòng chữ thuần (không dùng mẫu log có tham số nên không thể "rơi" dòng vì lệch số tham số) + CSV. Món review/weak/none kèm 3 ứng viên tốt nhất
/// (fdcId, dataType, mô tả, điểm, kcal/P/F/C so với catalog) để người duyệt chọn <c>--fdc key=fdcId</c>.
/// </summary>
public static class UsdaVerifyReportFormatter
{
    public static IEnumerable<string> Lines(UsdaVerifyReport r, bool apply)
    {
        static string Diffs(IEnumerable<UsdaDiff> d) => string.Join(", ", d.Select(x => FormattableString.Invariant($"{x.Field} {x.Ours:0.##}→{x.Usda:0.##} ({x.Percent:P0})")));
        static string Ours(UsdaVerifyItem i) => FormattableString.Invariant($"catalog: kcal {i.OurKcal:0.#} P {i.OurProtein:0.#} F {i.OurFat:0.#} C {i.OurCarbs:0.#}");
        static string Macros(UsdaVerifyCandidate c) => $"kcal {N(c.Kcal)} P {N(c.Protein)} F {N(c.Fat)} C {N(c.Carbs)}";
        static string N(decimal? v) => v is { } x ? x.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) : "?";

        foreach (var i in r.Items.Where(i => i.Status == UsdaVerifyStatus.Match))
            yield return FormattableString.Invariant($"  MATCH  {i.Key} '{i.Name}' ↔ FDC {i.FdcId} '{i.FdcDescription}' [{i.DataType}] điểm {i.Score:0.00}{(i.Applied ? " ĐÃ GHI" : "")}: macro lệch {(i.MacroDiffs.Count == 0 ? "không" : Diffs(i.MacroDiffs))}; chất phụ lệch {(i.NutrientDiffs.Count == 0 ? "không" : Diffs(i.NutrientDiffs))}");

        foreach (var i in r.Items.Where(i => i.Status is UsdaVerifyStatus.Review or UsdaVerifyStatus.Weak))
        {
            var why = i.Status == UsdaVerifyStatus.Review ? $"tên khớp nhưng số liệu lệch bất thường ({Diffs(i.MacroDiffs)})" : "khớp tên yếu";
            yield return $"  {(i.Status == UsdaVerifyStatus.Review ? "REVIEW" : "WEAK  ")} {i.Key} '{i.Name}' ({Ours(i)}): {why}; KHÔNG ghi. Đúng ứng viên nào thì chạy lại với --fdc {i.Key}=<fdcId>";
            if (i.Candidates is not { Count: > 0 }) yield return "      (không có ứng viên từ FDC)";
            else
                foreach (var (c, n) in i.Candidates.Select((c, n) => (c, n + 1)))
                    yield return FormattableString.Invariant($"      {n}. FDC {c.FdcId} [{c.DataType}] '{c.Description}' điểm {c.Score:0.00}{(c.Mismatch is null ? "" : $" (loại: {c.Mismatch})")} · {Macros(c)}");
        }

        foreach (var i in r.Items.Where(i => i.Status == UsdaVerifyStatus.None))
            yield return $"  KHÔNG TÌM THẤY {i.Key} '{i.Name}' ({i.Provider}): {(i.Provider == CanonicalCatalogData.VnProvider ? "không có tương đương USDA, giữ nguồn bảng VN (Unverified)" : "không có tương đương USDA, giữ nguyên")}"
                + (i.Candidates is { Count: > 0 } ? $"; gần nhất FDC {i.Candidates[0].FdcId} '{i.Candidates[0].Description}'" : "");
        foreach (var i in r.Items.Where(i => i.Status == UsdaVerifyStatus.Error))
            yield return $"  LỖI    {i.Key} '{i.Name}': {i.Error ?? "không gọi được FDC (mạng hoặc hết hạn mức); chạy lại sau"}";

        yield return $"Tổng {r.Total} món, {r.Requests} request FDC: match {r.Count(UsdaVerifyStatus.Match)} · review {r.Count(UsdaVerifyStatus.Review)} · weak {r.Count(UsdaVerifyStatus.Weak)} · không tìm thấy {r.Count(UsdaVerifyStatus.None)} · đã Verified/bỏ qua {r.Count(UsdaVerifyStatus.Already)} · lỗi {r.Count(UsdaVerifyStatus.Error)} · chưa kiểm (hết trần request) {r.Count(UsdaVerifyStatus.NotChecked)}";
        yield return apply
            ? $"Đã ghi {r.Items.Count(i => i.Applied)} món (một transaction): macro/chất phụ theo USDA, IsVerified, Source = USDA, nguồn FDC trong FoodSourceRecord."
            : "Dry-run xong, chưa ghi gì. Backup rồi chạy lại với: -- verify-usda --apply --backed-up";
        if (r.Count(UsdaVerifyStatus.NotChecked) + r.Count(UsdaVerifyStatus.Error) > 0)
            yield return "Còn món chưa kiểm/lỗi: đợi hết giờ hạn mức rồi chạy lại cùng lệnh (món đã xác minh được bỏ qua).";
    }

    /// <summary>CSV mọi món đã kiểm (trừ đã Verified): mỗi ứng viên một dòng, để duyệt bằng bảng tính.</summary>
    public static void WriteCsv(string path, UsdaVerifyReport r)
    {
        static string Q(string? s) => $"\"{(s ?? "").Replace("\"", "\"\"")}\"";
        static string N(decimal? v) => v is { } x ? x.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) : "";
        var sb = new System.Text.StringBuilder("status,key,name,provider,our_kcal,our_protein,our_fat,our_carbs,rank,fdc_id,data_type,description,score,mismatch,kcal,protein,fat,carbs,error\n");
        foreach (var i in r.Items.Where(i => i.Status != UsdaVerifyStatus.Already))
        {
            var head = $"{i.Status},{Q(i.Key)},{Q(i.Name)},{i.Provider},{N(i.OurKcal)},{N(i.OurProtein)},{N(i.OurFat)},{N(i.OurCarbs)}";
            if (i.Candidates is not { Count: > 0 }) { sb.Append(head).Append(",,,,,,,,,,,").Append(Q(i.Error)).Append('\n'); continue; }
            foreach (var (c, n) in i.Candidates.Select((c, n) => (c, n + 1)))
                sb.Append(head).Append(',').Append(n).Append(',').Append(c.FdcId).Append(',').Append(Q(c.DataType)).Append(',').Append(Q(c.Description)).Append(',')
                    .Append(N(c.Score)).Append(',').Append(Q(c.Mismatch)).Append(',').Append(N(c.Kcal)).Append(',').Append(N(c.Protein)).Append(',').Append(N(c.Fat)).Append(',').Append(N(c.Carbs)).Append(',').Append(Q(i.Error)).Append('\n');
        }
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path, sb.ToString(), new System.Text.UTF8Encoding(true));
    }
}
