namespace Application;

/// <summary>
/// Gom FoodItem mới nhập (USDA/OFF) vào thực phẩm chuẩn thay vì để dòng rời. Khớp CHÍNH XÁC theo thứ tự: barcode → tên chuẩn hoá → alias;
/// không khớp thì tạo canonical mới với chính món này làm đại diện. Không gộp theo độ giống (tránh "Beef" nuốt "Beef, bone").
/// Món do user tạo là riêng tư nên không gom. Chưa lưu (caller SaveChanges).
/// </summary>
public static class CanonicalLinker
{
    /// <summary>Nhỏ = ưu tiên khi chọn nguồn hiển thị: seed hệ thống &lt; USDA &lt; bảng VN &lt; Open Food Facts.</summary>
    public static int Priority(FoodItem f) => f.Source switch
    {
        FoodSource.System => 5,
        FoodSource.USDA => f.IsVerified ? 10 : 15,
        FoodSource.VnFct => 20,
        FoodSource.OpenFoodFacts => 30,
        _ => 40,
    };

    public static string Provider(FoodItem f) => f.ProviderCode ?? f.Source switch
    {
        FoodSource.USDA => "usda",
        FoodSource.OpenFoodFacts => "off",
        FoodSource.VnFct => "vn-fct",
        _ => "seed",
    };

    public static async Task LinkAsync(IUnitOfWork uow, IReadOnlyCollection<FoodItem> foods, CancellationToken ct)
    {
        var todo = foods.Where(f => f.CanonicalFoodId is null && f.Source != FoodSource.UserCreated && f.Source != FoodSource.QuickAdd).ToList();
        if (todo.Count == 0) return;

        var norms = todo.Select(f => FoodNameNormalizer.Normalize(f.Name)).Where(n => n.Length > 0).Distinct().ToList();
        var barcodes = todo.Where(f => !string.IsNullOrEmpty(f.Barcode)).Select(f => f.Barcode!).Distinct().ToList();

        var byName = await uow.Repository<CanonicalFood>().Query()
            .Where(c => norms.Contains(c.NameNormalized)).Select(c => new { c.NameNormalized, c.Id })
            .ToDictionaryAsync(c => c.NameNormalized, c => c.Id, ct);
        var byAlias = (await uow.Repository<FoodAlias>().Query()
                .Where(a => a.CanonicalFoodId != null && norms.Contains(a.AliasNormalized)).Select(a => new { a.AliasNormalized, Id = a.CanonicalFoodId!.Value })
                .ToListAsync(ct))
            .GroupBy(a => a.AliasNormalized).ToDictionary(g => g.Key, g => g.First().Id);
        var byBarcode = barcodes.Count == 0 ? [] : (await uow.Repository<FoodItem>().Query()
                .Where(f => f.Barcode != null && barcodes.Contains(f.Barcode) && f.CanonicalFoodId != null).Select(f => new { f.Barcode, Id = f.CanonicalFoodId!.Value })
                .ToListAsync(ct))
            .GroupBy(f => f.Barcode!).ToDictionary(g => g.Key, g => g.First().Id);

        foreach (var food in todo)
        {
            var norm = FoodNameNormalizer.Normalize(food.Name);
            Guid canonicalId;
            string method;
            decimal confidence;
            var isNew = false;

            if (food.Barcode is { Length: > 0 } code && byBarcode.TryGetValue(code, out var viaBarcode)) { canonicalId = viaBarcode; method = "barcode"; confidence = 1m; }
            else if (norm.Length > 0 && byName.TryGetValue(norm, out var viaName)) { canonicalId = viaName; method = "name"; confidence = 1m; }
            else if (norm.Length > 0 && byAlias.TryGetValue(norm, out var viaAlias)) { canonicalId = viaAlias; method = "alias"; confidence = 0.9m; }
            else
            {
                canonicalId = Guid.NewGuid();
                method = "new";
                confidence = 1m;
                isNew = true;
                await uow.Repository<CanonicalFood>().AddAsync(new CanonicalFood
                {
                    Id = canonicalId,
                    Name = food.Name,
                    NameNormalized = norm.Length > 0 ? norm : canonicalId.ToString("N"),
                    Category = food.Category,
                }, ct);
                if (norm.Length > 0) byName[norm] = canonicalId; // món thứ hai cùng tên trong lô gom vào canonical vừa tạo
            }

            food.CanonicalFoodId = canonicalId;
            food.IsRepresentative = isNew;
            await uow.Repository<FoodSourceRecord>().AddAsync(new FoodSourceRecord
            {
                Id = Guid.NewGuid(), CanonicalFoodId = canonicalId, FoodItemId = food.Id, Provider = Provider(food), ExternalId = food.ExternalFoodId,
                Priority = Priority(food), MatchConfidence = confidence, MatchMethod = method,
            }, ct);
        }
    }
}
