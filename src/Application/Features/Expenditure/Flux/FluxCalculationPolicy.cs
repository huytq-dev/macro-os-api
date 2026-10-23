namespace Application;

/// <summary>
/// Chính sách Flux có phiên bản (MACROOS POLICY, KHÔNG phải công thức MacroFactor V3 — MF chưa công bố). Dải = bao của họ quỹ đạo ứng viên (độ nhạy chậm … nhanh) chạy trên CÙNG quan sát với bộ ước lượng production.
/// Tham số chỉ được chọn bằng hiệu chỉnh trên dữ liệu tổng hợp có sự thật nền và kiểm định trên kịch bản giữ lại (xem Docs/Design/Flux), không chỉnh theo hình biểu đồ MF.
/// </summary>
public sealed record FluxCalculationPolicy(string Version, IReadOnlyList<FluxCandidateSpec> Candidates)
{
    public const string V1Version = "MACROOS_FLUX_V1";

    /// <summary>
    /// ỨNG VIÊN ĐỀ XUẤT, CHƯA ĐƯỢC DUYỆT, chưa có caller production. Giá trị = lựa chọn C3 của hiệu chỉnh ở Docs/Design/Flux (Report E; 20 seed hiệu chỉnh, chỉ trên kịch bản S01/S02/S04/S06/S09/S10):
    /// β chậm 0.07 / nhanh 0.14 và cửa sổ ngắn 14 / dài 56 quanh production (β 0.1, W 21). KẾT QUẢ KIỂM ĐỊNH: dải này KHÔNG theo dõi được độ lớn thay đổi thật (ρ giữ lại ≈ 0.07), nên chưa đủ điều kiện kích hoạt; cần quyết định của user.
    /// </summary>
    public static FluxCalculationPolicy V1 { get; } = new(V1Version,
    [
        new FluxCandidateSpec("slow", 0.07m, 21),
        new FluxCandidateSpec("fast", 0.14m, 21),
        new FluxCandidateSpec("short", 0.10m, 14),
        new FluxCandidateSpec("long", 0.10m, 56),
    ]);
}
