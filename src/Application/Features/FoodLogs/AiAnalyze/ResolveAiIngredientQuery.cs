namespace Application;

/// <summary>
/// Pha 2 của AI Plate: tìm một nguyên liệu (tên tiếng Anh hoặc Việt + gram) trong DB thực phẩm và tính macro từ món khớp.
/// Thứ tự: bộ nhớ đệm của user (món đã xác nhận, khoá có thêm state) → thực phẩm chuẩn (qua alias/tên chuẩn hoá; chọn đúng biến thể theo tên hẳn hoặc state, không có thì dòng đại diện)
/// → tìm rộng có xếp hạng. Không khớp → AI ước lượng (Estimated=true) hoặc 0 (không bịa số). Không ghi nhật ký (chỉ cập nhật HitCount của bộ nhớ đệm khi trúng).
/// <paramref name="Quantity"/> + <paramref name="Unit"/> (tuỳ chọn, từ AI): đếm được ("3 large") thì chọn khẩu phần có đúng tên đơn vị; không có/không khớp thì chọn khẩu phần theo gram như cũ.
/// <paramref name="State"/> (tuỳ chọn): raw | boiled | fried | scrambled | grilled | steamed | baked | roasted | stir_fried | none.
/// </summary>
public sealed record ResolveAiIngredientQuery(string Name, decimal Grams, decimal? Quantity = null, string? Unit = null, string? State = null) : IRequest<AiResolvedIngredientModel>;
