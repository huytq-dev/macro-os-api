namespace Application;

/// <summary>
/// Worker dựng lại dữ liệu DẪN XUẤT nền (snapshot expenditure/daily progress) từ hàng đợi bền <see cref="RecalculationJob"/>.
/// Việc dựng ngắn sau khi sửa dữ liệu nguồn chạy đồng bộ (<see cref="DerivedDataSync"/>); phần vượt ngân sách, lỗi, hoặc không lấy được khoá thì thành job trong DB và worker xử lý,
/// có thử lại, an toàn khi restart và nhiều instance. Interface này chỉ để đánh thức worker của instance hiện tại; job nằm trong DB nên instance khác (hoặc lần quét kế tiếp) cũng nhận được.
/// </summary>
public interface IDerivedDataScheduler
{
    /// <summary>Có job mới vừa được lưu: quét ngay thay vì chờ chu kỳ. Không làm gì nếu không có worker (test, công cụ dòng lệnh).</summary>
    void Wake();

    /// <summary>Ghi log (Information, không có dữ liệu cá nhân) thời gian của một lần dựng đồng bộ sau khi ghi dữ liệu: để kiểm chi phí sau khi deploy.</summary>
    /// <param name="outcome">Completed (đã mới hoàn toàn) | Queued (còn job nền) | Failed (lỗi, đã xếp job).</param>
    void ReportSync(string cause, long elapsedMs, int rowsWritten, string outcome);
}
