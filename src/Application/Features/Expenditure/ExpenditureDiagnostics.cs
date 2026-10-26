namespace Application;

/// <summary>Kết quả chẩn đoán "vì sao chưa Updating" cho một ngày: mã lý do, câu lý do cho UI và số ngày dự kiến để resume. <see cref="Codes"/> rỗng = đủ điều kiện (đang/được phép Updating).</summary>
public sealed record ExpenditureDiagnosis(ExpenditureStatusMetrics Metrics, IReadOnlyList<string> Codes, string? Reason, int? ResumeInDays);

/// <summary>
/// MỘT nguồn lý do cho Holding VÀ Calibrating (trước đây chỉ có khi status = Holding và chỉ phản ánh quy tắc 4/7 log + 1/7 cân). Thêm hai lý do mà máy trạng thái không thấy nhưng khiến
/// <see cref="ExpenditureRebuilder"/> hạ Updating → Holding: <see cref="ExpenditureStatusMachine.CodeNeedsTwoWeighIns"/> (không đủ 2 lần cân có trend trong cửa sổ tính, kể từ ngày bắt đầu) và
/// <see cref="ExpenditureStatusMachine.CodeNoLogsBetweenWeighIns"/> (có ≥ 2 lần cân nhưng không ngày log hợp lệ nào nằm giữa lần cân đầu và cuối).
/// </summary>
public static class ExpenditureDiagnostics
{
    /// <summary>
    /// Nạp đúng dữ liệu mà ExpenditureRebuilder dùng cho ngày <paramref name="end"/>: cửa sổ tính (W ngày kết thúc ở end, từ ngày bắt đầu) và 8 ngày trạng thái (ngày trước ngày bắt đầu coi như không có dữ liệu).
    /// Lần cân tính khi có TrendWeightKg (đúng như rebuilder), ngày log tính khi AlgorithmKcal có giá trị (nhịn ăn hợp lệ, kcal &gt; 0, không đánh dấu chưa đủ).
    /// </summary>
    public static async Task<(List<ExpenditureDay> StatusDays, List<ExpenditureDay> Window)> LoadAsync(IUnitOfWork uow, Guid userId, DateOnly end, DateOnly? start, AlgorithmOptions opt, CancellationToken ct)
    {
        var from = end.AddDays(-(Math.Max(opt.ExpenditureWindowDays, 8) - 1));
        var logs = await FoodLogSupport.LoadDaysAsync(uow, userId, from, end, ct, includeFasting: true);
        var trends = await uow.Repository<WeightEntry>().Query()
            .Where(w => w.UserId == userId && w.LogDate >= from && w.LogDate <= end && w.TrendWeightKg != null)
            .Select(w => new { w.LogDate, Trend = w.TrendWeightKg!.Value }).ToDictionaryAsync(w => w.LogDate, w => w.Trend, ct);
        var days = Enumerable.Range(0, end.DayNumber - from.DayNumber + 1).Select(i => from.AddDays(i))
            .Select(d => new ExpenditureDay(d, FoodLogSupport.AlgorithmKcalOn(logs, d), trends.TryGetValue(d, out var t) ? t : null)).ToList();

        var window = days.TakeLast(opt.ExpenditureWindowDays).Where(d => start is not { } s || d.Date >= s).ToList();
        var statusDays = days.TakeLast(8).Select(d => start is { } s && d.Date < s ? new ExpenditureDay(d.Date, null, null) : d).ToList();
        return (statusDays, window);
    }

    public const string CodeInitialCalibration = "InitialCalibration";

    /// <param name="calibrationDaysLeft">Số ngày còn lại của giai đoạn đóng băng TDEE ban đầu (FreezeTdeeDuringCalibration); null = không đóng băng.</param>
    public static ExpenditureDiagnosis Diagnose(IReadOnlyList<ExpenditureDay> statusDays, IReadOnlyList<ExpenditureDay> window, DateOnly today, int windowDays, int? calibrationDaysLeft = null)
    {
        var m = ExpenditureStatusMachine.Metrics(statusDays, today);
        var codes = ExpenditureStatusMachine.HoldingReasonCodes(m);
        var reason = ExpenditureStatusMachine.HoldingReason(m);
        // Đủ điều kiện 4/7 log + 1/7 cân nhưng vẫn không tính được TDEE mới: lý do nằm ở khoảng cân (rebuilder hạ về Holding)
        if (codes.Count == 0 && ExpenditureStatusMachine.RawBlocker(window, windowDays) is { } blocker)
        {
            codes = [blocker.Code];
            reason = blocker.Message;
        }
        // Giai đoạn đầu TDEE bị đóng băng theo thiết kế: nói rõ thay vì để trống (mã đứng đầu, kèm các lý do dữ liệu nếu có)
        if (calibrationDaysLeft is > 0 and var left)
        {
            var frozen = $"Đang hiệu chỉnh ban đầu: TDEE giữ nguyên thêm {left} ngày";
            codes = [CodeInitialCalibration, .. codes];
            reason = reason is null ? frozen : $"{frozen}; {reason}";
        }
        return new ExpenditureDiagnosis(m, codes, reason, codes.Count == 0 ? null : ExpenditureStatusMachine.ResumeInDays(m));
    }
}
