namespace FluxResearch;

/// <summary>18 kịch bản của user. Tập HIỆU CHỈNH (Calibration = true): S01, S02, S04, S06, S09, S10. Phần còn lại (12 kịch bản) là GIỮ LẠI, không được dùng để chọn tham số.</summary>
public static class Scenarios
{
    private static Func<int, double> Step(double before, double after, int day) => d => d < day ? before : after;
    private static Func<int, double> Ramp(double from, double to, int d0, int d1) => d => d <= d0 ? from : d >= d1 ? to : from + (to - from) * (d - d0) / (d1 - d0);

    public static IReadOnlyList<Scenario> All { get; } =
    [
        new() { Id = "S01", Name = "Stable 2500", Calibration = true, Description = "TDEE hằng 2500, nhiễu log/cân bình thường" },
        new() { Id = "S02", Name = "Ramp 2400→2800", Calibration = true, Truth = Ramp(2400, 2800, 30, 90), EventDay = 30, Description = "tăng tuyến tính ngày 30–90" },
        new() { Id = "S03", Name = "Ramp 2800→2300", Calibration = false, Truth = Ramp(2800, 2300, 30, 90), EventDay = 30, Description = "giảm tuyến tính ngày 30–90" },
        new() { Id = "S04", Name = "Step up 2400→2800", Calibration = true, Truth = Step(2400, 2800, 60), EventDay = 60, Description = "bước tăng ngày 60" },
        new() { Id = "S05", Name = "Step down 2800→2400", Calibration = false, Truth = Step(2800, 2400, 60), EventDay = 60, Description = "bước giảm ngày 60" },
        new() { Id = "S06", Name = "Water retention", Calibration = true, ScaleShift = d => d < 50 ? 0 : 1.8 * Math.Pow(0.5, (d - 50) / 6.0), Description = "TDEE hằng; +1.8 kg nước ngày 50, nửa đời 6 ngày" },
        new() { Id = "S07", Name = "Glycogen shifts", Calibration = false, ScaleShift = d => Smooth(d, 40, 43, 1.2) - Smooth(d, 60, 63, 1.2) + Smooth(d, 90, 93, 1.2) - Smooth(d, 110, 113, 1.2), Description = "TDEE hằng; ±1.2 kg trong 3 ngày (low-carb ↔ refeed) hai lần" },
        new() { Id = "S08", Name = "Plateau + whoosh", Calibration = false, Deficit = _ => 500, ScaleShift = d => d < 30 ? 0 : d < 61 ? 2.4 * (d - 30) / 31.0 : d < 64 ? 2.4 * (64 - d) / 3.0 : 0, Description = "thâm hụt thật 500; nước tích 2.4 kg che giảm cân (ngày 30–60) rồi tụt trong 3 ngày" },
        new() { Id = "S09", Name = "Missing logs", Calibration = true, LogValidP = 0.65, NoLog = d => d is >= 70 and <= 76, Description = "35% ngày không log + khoảng trống 7 ngày (70–76)" },
        new() { Id = "S10", Name = "Long weigh-in gap", Calibration = true, NoWeigh = d => d is >= 50 and <= 74, Description = "không cân 25 ngày (50–74)" },
        new() { Id = "S11", Name = "Holding → Updating", Calibration = false, Truth = Step(2500, 2700, 55), EventDay = 55, NoLog = d => d is >= 50 and <= 62, LogValidP = 1.0, Description = "mất log 13 ngày (Holding) trong đó TDEE thật tăng +200 ngày 55, rồi log lại" },
        new() { Id = "S12a", Name = "Calibrating → Updating (frozen 14d)", Calibration = false, InitialTdeeError = -0.12, Truth = _ => 2500, Description = "TDEE₀ thấp 12 %, đóng băng 14 ngày (mặc định production)" },
        new() { Id = "S12b", Name = "Calibrating → Updating (learn from day 1)", Calibration = false, InitialTdeeError = -0.12, FreezeCalibration = false, Description = "như S12a nhưng 'học từ ngày 1' (cờ chưa duyệt)" },
        new() { Id = "S13", Name = "Static mode (overlay không nối)", Calibration = false, StreamKey = "S01", Description = "dữ liệu GIỐNG HỆT S01; user đặt Static từ ngày 60. Flux chỉ thuộc bộ học ⇒ phải bằng S01" },
        new() { Id = "S14", Name = "Goal change", Calibration = false, Deficit = d => d < 60 ? 300 : 0, EventDay = null, Description = "đổi goal ngày 60 (thâm hụt 300 → 0), TDEE thật hằng, không reset TDEE" },
        new() { Id = "S15", Name = "Program change, no TDEE reset", Calibration = false, StreamKey = "S01", Description = "dữ liệu GIỐNG HỆT S01; đổi program ngày 70 (không vào engine) ⇒ phải bằng S01" },
        new() { Id = "S16", Name = "Backdated log", Calibration = false, BackfilledLater = d => d is >= 40 and <= 46, Description = "7 ngày log (40–46) nhập muộn ở ngày 100: so sánh trước/sau" },
        new() { Id = "S17", Name = "Sparse weigh-ins (weekly)", Calibration = false, WeighOnly = d => d % 7 == 0, Description = "cân đúng 1 lần/tuần" },
        new() { Id = "S18", Name = "No valid observations", Calibration = false, NoObservations = true, Description = "không có log, không có cân" },
    ];

    /// <summary>Tăng mượt 0 → amp trong khoảng [from, to] ngày (dùng cho glycogen).</summary>
    private static double Smooth(int d, int from, int to, double amp) => d < from ? 0 : d >= to ? amp : amp * (d - from) / (double)(to - from);
}
