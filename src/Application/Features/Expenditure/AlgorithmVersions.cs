namespace Application;

/// <summary>
/// Phiên bản thuật toán GHI vào dữ liệu dẫn xuất (chỉ theo dõi nội bộ, không hiện trên UI). Hai chuỗi phiên bản TÁCH RỜI; định nghĩa từng phiên bản ở Docs/Design/Algorithm_Versions.md.
/// Dòng cũ chưa được dựng lại KHÔNG bị gắn nhãn: ExpenditureSnapshot.AlgorithmVersion mặc định "1" và WeightEntry.TrendAlgorithmVersion null đều nghĩa là Legacy/Unknown, không đoán.
/// </summary>
public static class AlgorithmVersions
{
    /// <summary>Phiên bản công thức Expenditure v6 = v4 + sửa EXP-001 (lượng ăn chỉ lấy trong khoảng [lần cân đầu, lần cuối] cùng khoảng với Δtrend). Cũ: v4 (= v2 + máy trạng thái Updating/Holding/Calibrating có trạng thái, resume 3 ngày) = quy tắc 0 kcal + Cunningham khởi điểm, TDEE vẫn đóng băng giai đoạn Calibrating (ExpenditureSnapshot.AlgorithmVersion, RecalculationJob.AlgorithmVersion).</summary>
    public const string Expenditure = "6";

    /// <summary>Expenditure v7 = v6 + "học từ ngày 1" (#3). (v4/v5 = trước sửa EXP-001: lượng ăn trung bình cả cửa sổ 21 ngày, không khớp khoảng cân.) Cũ: v5 (= v3 + máy trạng thái mới) = v4 + "học từ ngày 1" (#3, bỏ đóng băng); chỉ ghi khi <c>FreezeTdeeDuringCalibration = false</c>.</summary>
    public const string ExpenditureLearnFromDayOne = "7";

    /// <summary>Nhãn của công thức đang chạy theo cấu hình: snapshot được gắn đúng phiên bản đã thực sự tính ra nó.</summary>
    public static string ExpenditureFor(AlgorithmOptions opt) => opt.FreezeTdeeDuringCalibration ? Expenditure : ExpenditureLearnFromDayOne;

    /// <summary>Phiên bản Weight Trend / weight change rate (WeightEntry.TrendAlgorithmVersion).</summary>
    public const string WeightTrend = "2";

    /// <summary>Giá trị mặc định của cột cũ: dữ liệu từ trước khi có gắn nhãn, không rõ công thức đã dùng.</summary>
    public const string Legacy = "1";

    /// <summary>Số của phiên bản; null/không đọc được = 0 (Legacy/Unknown, cũ hơn mọi phiên bản có nhãn).</summary>
    public static int Number(string? version) => int.TryParse(version, out var n) ? n : 0;

    /// <summary>true khi <paramref name="existing"/> MỚI HƠN <paramref name="current"/>: kết quả của phiên bản cũ không được ghi đè.</summary>
    public static bool IsNewer(string? existing, string current) => Number(existing) > Number(current);
}
