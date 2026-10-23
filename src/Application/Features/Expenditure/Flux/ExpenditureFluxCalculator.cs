namespace Application;

/// <summary>
/// Flux Range (MACROOS_FLUX_V1, hàm thuần, xác định, nhân quả: ngày d chỉ dùng quan sát có ngày ≤ d; KHÔNG ghi gì, KHÔNG phản hồi vào TDEE đã học, KHÔNG đổi máy trạng thái G0).
/// Mỗi quỹ đạo ứng viên chạy đúng cấu trúc production (<see cref="ExpenditureCalculator.ComputeRaw"/> trên cửa sổ W ngày rồi <see cref="ExpenditureCalculator.Smooth"/> hệ số β) nhưng khác (β, W);
/// chỉ cập nhật ở những ngày production đang Updating (Holding / Calibrating = không có quan sát mới, ứng viên đứng yên). Dải = bao min/max của mọi ứng viên VÀ TDEE đã học
/// nên luôn chứa TDEE đã học; độ rộng do dữ liệu quyết định (các độ nhạy khác nhau bất đồng khi expenditure thật đổi), không có ± kcal hay % cố định.
/// Đây là CHÍNH SÁCH MACROOS tìm cách tái hiện ý "không gian quỹ đạo khả dĩ" của MF; KHÔNG khẳng định giống hệt MF V3 và KHÔNG phải khoảng tin cậy.
/// </summary>
public static class ExpenditureFluxCalculator
{
    /// <param name="observations">Danh sách ngày LIÊN TỤC từ <see cref="ExpenditureObservationResolver"/> (có thể bắt đầu trước <paramref name="start"/>; ngày &lt; start bị bỏ như rebuilder).</param>
    /// <param name="learned">Chuỗi TDEE đã học liên tục theo ngày, phần tử đầu = snapshot ngày bắt đầu (TDEE₀).</param>
    public static IReadOnlyList<FluxPoint> Compute(IReadOnlyList<ExpenditureDay> observations, IReadOnlyList<FluxLearnedDay> learned, DateOnly start, FluxCalculationPolicy policy, int kcalPerKg)
    {
        if (learned.Count == 0) return [];
        return Band(learned, policy.Candidates.Select(c => Trajectory(observations, learned, start, c, kcalPerKg)).ToList());
    }

    /// <summary>Bước bao min/max (thuần): từ các quỹ đạo ứng viên đã tính (mỗi quỹ đạo dài bằng <paramref name="learned"/>) và chuỗi đã học → dải từng ngày. Tách riêng để nghiên cứu có thể dùng lại quỹ đạo đã tính.</summary>
    public static IReadOnlyList<FluxPoint> Band(IReadOnlyList<FluxLearnedDay> learned, IReadOnlyList<decimal[]> paths)
    {
        var points = new List<FluxPoint>(learned.Count);
        var everUpdating = false;
        for (var k = 0; k < learned.Count; k++)
        {
            var day = learned[k];
            if (day.Status == ExpenditureStatus.Updating) everUpdating = true;
            var state = day.Status switch
            {
                ExpenditureStatus.Calibrating => FluxBandState.Calibrating,
                ExpenditureStatus.Updating => FluxBandState.Updating,
                _ => everUpdating ? FluxBandState.Holding : FluxBandState.NoObservations,
            };
            if (state is FluxBandState.Calibrating or FluxBandState.NoObservations) { points.Add(new FluxPoint(day.Date, null, null, state, [])); continue; }

            var values = paths.Select(p => p[k]).ToList();
            points.Add(new FluxPoint(day.Date, Math.Floor(Math.Min(values.DefaultIfEmpty(day.TdeeKcal).Min(), day.TdeeKcal)), Math.Ceiling(Math.Max(values.DefaultIfEmpty(day.TdeeKcal).Max(), day.TdeeKcal)), state, values));
        }
        return points;
    }

    /// <summary>Quỹ đạo của MỘT ứng viên (kcal nguyên, chưa làm tròn) tại mọi ngày của <paramref name="learned"/>. Khởi tạo = TDEE đã học ngày đầu; cập nhật chỉ khi production Updating và ứng viên tính được raw.</summary>
    public static decimal[] Trajectory(IReadOnlyList<ExpenditureDay> observations, IReadOnlyList<FluxLearnedDay> learned, DateOnly start, FluxCandidateSpec spec, int kcalPerKg)
    {
        var byDate = observations.Where(d => d.Date >= start).ToDictionary(d => d.Date);
        var path = new decimal[learned.Count];
        var current = learned[0].TdeeKcal;
        for (var k = 0; k < learned.Count; k++)
        {
            if (k > 0 && learned[k].Status == ExpenditureStatus.Updating)
            {
                var raw = ExpenditureCalculator.ComputeRaw(Window(byDate, learned[k].Date, spec.WindowDays, start), kcalPerKg);
                if (raw is not null) current = ExpenditureCalculator.Smooth(current, raw.TdeeRaw, spec.Beta);
            }
            path[k] = current;
        }
        return path;
    }

    private static List<ExpenditureDay> Window(IReadOnlyDictionary<DateOnly, ExpenditureDay> byDate, DateOnly end, int windowDays, DateOnly start)
    {
        var first = end.AddDays(-(windowDays - 1));
        if (first < start) first = start;
        var days = new List<ExpenditureDay>(windowDays);
        for (var d = first; d <= end; d = d.AddDays(1))
            days.Add(byDate.TryGetValue(d, out var day) ? day : new ExpenditureDay(d, null, null));
        return days;
    }
}
