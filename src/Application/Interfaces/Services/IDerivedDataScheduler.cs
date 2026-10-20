namespace Application;

/// <summary>
/// Dựng lại dữ liệu DẪN XUẤT sau khi ghi dữ liệu nguồn (entry food log, cân nặng): aggregate dinh dưỡng theo ngày (<see cref="NutritionAggregates"/>)
/// và snapshot expenditure/daily progress (<see cref="ExpenditureRebuilder"/>). Phần này cần ~10 truy vấn; trên DB ở xa mỗi truy vấn là một vòng đi về mạng
/// (~200 ms) nên làm đồng bộ khiến POST entries/weights mất 3–5 giây dù tính toán chỉ vài ms. Bản nền chạy SAU khi phản hồi đã trả về, gom theo user và chạy tuần tự.
/// Kết quả cuối cùng giống hệt bản đồng bộ; chỉ khác là các bảng dẫn xuất trễ vài giây (nguồn: totals ngày của FoodLog, cân nặng đã đúng ngay khi phản hồi).
/// </summary>
public interface IDerivedDataScheduler
{
    /// <summary>false = không có chạy nền (bản Noop dùng trong test/công cụ dòng lệnh): caller dựng đồng bộ như trước.</summary>
    bool IsAvailable { get; }

    /// <summary>
    /// Xếp hàng dựng lại cho <paramref name="userId"/> từ <paramref name="fromDate"/> đến hôm nay. <paramref name="nutritionDates"/> khác null = cũng làm mới aggregate dinh dưỡng của các ngày đó.
    /// Nhiều yêu cầu liên tiếp của cùng user được gộp (ngày sớm nhất, hợp các ngày).
    /// </summary>
    void Schedule(Guid userId, DateOnly fromDate, IReadOnlyCollection<DateOnly>? nutritionDates);
}
