using Application;
using Domain;

namespace FluxResearch;

/// <summary>Một kịch bản tổng hợp có SỰ THẬT NỀN (TDEE thật theo ngày). Mọi hàm nhận chỉ số ngày d (0 = ngày bắt đầu program).</summary>
public sealed record Scenario
{
    /// <summary>Nhân độ lớn mọi nhiễu (log, nước, cân, tuân thủ): 1 = mặc định; dùng cho phân tích độ nhạy theo tỉ lệ tín hiệu/nhiễu.</summary>
    public double NoiseScale { get; init; } = 1.0;
    public required string Id { get; init; }
    public required string Name { get; init; }
    /// <summary>true = dùng để HIỆU CHỈNH tham số; false = kịch bản GIỮ LẠI, chỉ dùng kiểm định.</summary>
    public required bool Calibration { get; init; }
    public string Description { get; init; } = "";
    public int Days { get; init; } = 150;
    /// <summary>Hai kịch bản cùng StreamKey dùng cùng chuỗi ngẫu nhiên (để so sánh bằng nhau chính xác, vd Static / đổi program so với Stable).</summary>
    public string? StreamKey { get; init; }
    public Func<int, double> Truth { get; init; } = _ => 2500;
    /// <summary>Thâm hụt kế hoạch (kcal/ngày, dương = ăn ít hơn TDEE thật).</summary>
    public Func<int, double> Deficit { get; init; } = _ => 300;
    public double LogValidP { get; init; } = 0.93;
    public Func<int, bool>? NoLog { get; init; }
    public double WeighP { get; init; } = 0.9;
    public Func<int, bool>? WeighOnly { get; init; }
    public Func<int, bool>? NoWeigh { get; init; }
    /// <summary>kg cộng vào số cân trên cân (nước, glycogen), KHÔNG đổi khối lượng thật.</summary>
    public Func<int, double> ScaleShift { get; init; } = _ => 0;
    public Func<int, bool>? Fasting { get; init; }
    public Func<int, bool>? Incomplete { get; init; }
    public double InitialTdeeError { get; init; } = 0.06;
    public bool FreezeCalibration { get; init; } = true;
    public bool NoObservations { get; init; }
    /// <summary>Ngày thay đổi thật bắt đầu (cho chỉ số onset / phục hồi).</summary>
    public int? EventDay { get; init; }
    /// <summary>Các ngày mà log bị nhập MUỘN (kịch bản backdate): bản "trước" không có chúng.</summary>
    public Func<int, bool>? BackfilledLater { get; init; }
}

/// <summary>Kết quả sinh dữ liệu + chạy bản sao bộ ước lượng production.</summary>
public sealed record World(
    Scenario Scenario, int Seed, DateOnly Start, double[] Truth, IReadOnlyList<ExpenditureDay> Observations,
    IReadOnlyList<FluxLearnedDay> Learned, int[] WindowCounts, int KcalPerKg, double?[] RawWeight);

public static class WorldBuilder
{
    public static readonly DateOnly Start = new(2026, 1, 5);
    public const int KcalPerKg = 7700;
    public const int TrendDays = 20;

    private static double Gauss(Random r) => Math.Sqrt(-2 * Math.Log(1 - r.NextDouble())) * Math.Cos(2 * Math.PI * r.NextDouble());

    public static World Build(Scenario s, int seed, bool withBackfilled = true)
    {
        var key = s.StreamKey ?? s.Id;
        var rng = new Random(seed * 7919 + key.Aggregate(17, (a, c) => a * 31 + c));
        var truth = Enumerable.Range(0, s.Days).Select(s.Truth).ToArray();
        var logs = new Dictionary<DateOnly, FoodLogSupport.LoggedDay>();
        var entries = new List<(DateOnly Date, decimal Kg)>();
        var rawWeight = new double?[s.Days];
        double weight = 85, water = 0;
        for (var d = 0; d < s.Days; d++)
        {
            var date = Start.AddDays(d);
            // Thứ tự rút ngẫu nhiên CỐ ĐỊNH mỗi ngày, bất kể nhánh nào, để hai kịch bản cùng StreamKey cho dữ liệu giống hệt
            var ns = s.NoiseScale;
            var adherence = 1 + 0.04 * ns * Gauss(rng); var logNoise = 0.07 * ns * Gauss(rng); var logU = rng.NextDouble(); var weighU = rng.NextDouble(); var waterE = Gauss(rng); var measE = Gauss(rng);
            water = 0.6 * water + 0.35 * ns * waterE;
            var fasting = s.Fasting?.Invoke(d) ?? false;
            var actual = fasting ? 0.0 : (truth[d] - s.Deficit(d)) * adherence;
            var scale = weight + water + s.ScaleShift(d) + 0.1 * ns * measE;
            weight += (actual - truth[d]) / KcalPerKg;

            var noLog = s.NoObservations || (s.NoLog?.Invoke(d) ?? false) || logU > s.LogValidP || (!withBackfilled && (s.BackfilledLater?.Invoke(d) ?? false));
            if (!s.NoObservations && (s.Incomplete?.Invoke(d) ?? false))
                logs[date] = new FoodLogSupport.LoggedDay(date, (decimal)(actual * 0.5 * (1 + logNoise)), 0, 0, 0, 0, 0, IsIncomplete: true);
            else if (!s.NoObservations && fasting)
                logs[date] = new FoodLogSupport.LoggedDay(date, 0, 0, 0, 0, 0, 0, IsFasting: true);
            else if (!noLog)
                logs[date] = new FoodLogSupport.LoggedDay(date, (decimal)(actual * (1 + logNoise)), 0, 0, 0, 0, 0);

            var weigh = !s.NoObservations && (s.WeighOnly?.Invoke(d) ?? weighU <= s.WeighP) && !(s.NoWeigh?.Invoke(d) ?? false);
            if (weigh) { entries.Add((date, (decimal)scale)); rawWeight[d] = (double)(decimal)scale; }
        }

        var trend = WeightTrendCalculator.Compute(entries.Select(e => (e.Date, e.Kg)), TrendDays);
        var observations = ExpenditureObservationResolver.Daily(Start, Start.AddDays(s.Days - 1), logs, trend);
        var tdee0 = Math.Round((decimal)(truth[0] * (1 + s.InitialTdeeError)), 0);
        var (learned, windows) = ProductionReplica.Run(observations, Start, tdee0, s.FreezeCalibration);
        return new World(s, seed, Start, truth, observations, learned, windows, KcalPerKg, rawWeight);
    }
}

/// <summary>
/// Bản sao vòng lặp ngày của <c>ExpenditureRebuilder.cs:121-132</c> dùng CHÍNH các hàm production (<c>ExpenditureStatusMachine.Next</c>, <c>ExpenditureCalculator.ComputeRaw/Smooth/IsCalibrating</c>),
/// không DB. Dùng để có chuỗi TDEE đã học + status trong mô phỏng; KHÔNG thay thế rebuilder.
/// </summary>
public static class ProductionReplica
{
    public const decimal Beta = 0.1m;
    public const int WindowDays = 21;
    public const int CalibratingDays = 14;

    public static (List<FluxLearnedDay> Learned, int[] WindowCounts) Run(IReadOnlyList<ExpenditureDay> days, DateOnly start, decimal tdee0, bool freezeCalibration)
    {
        var learned = new List<FluxLearnedDay>(days.Count);
        var windows = new int[days.Count];
        var prevTdee = tdee0;
        ExpenditureStatus? prevStatus = null;
        for (var i = 0; i < days.Count; i++)
        {
            var d = days[i].Date;
            var window = days.Skip(Math.Max(0, i - WindowDays + 1)).Take(Math.Min(WindowDays, i + 1)).Where(x => x.Date >= start).ToList();
            var statusDays = days.Skip(Math.Max(0, i - 7)).Take(Math.Min(8, i + 1)).Where(x => x.Date >= start).ToList();
            var status = ExpenditureStatusMachine.Next(prevStatus, statusDays, DateOnly.MaxValue);
            if (freezeCalibration && ExpenditureCalculator.IsCalibrating(d, start, CalibratingDays)) status = ExpenditureStatus.Calibrating;
            var raw = status == ExpenditureStatus.Updating ? ExpenditureCalculator.ComputeRaw(window, WorldBuilder.KcalPerKg) : null;
            if (raw is null && status == ExpenditureStatus.Updating) status = ExpenditureStatus.Holding;
            prevStatus = status;
            var tdee = raw is null ? prevTdee : ExpenditureCalculator.Smooth(prevTdee, raw.TdeeRaw, Beta);
            learned.Add(new FluxLearnedDay(d, tdee, status));
            windows[i] = window.Count;
            prevTdee = tdee;
        }
        return (learned, windows);
    }
}
