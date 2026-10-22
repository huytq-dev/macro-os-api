namespace Contract;

/// <summary>
/// Trạng thái dựng lại dữ liệu dẫn xuất (snapshot expenditure, daily progress) của user; BE là nguồn sự thật. FE: Updating → hiện "đang cập nhật" và refetch theo nhịp tới khi Ready;
/// Failed → báo lỗi + cho thử lại (POST /derived-data/retry). Số trên màn hình luôn đúng với dữ liệu nguồn khi Ready.
/// </summary>
/// <param name="State">Ready | Updating | Failed.</param>
/// <param name="FromDate">Ngày đầu của khoảng đang dựng lại (null khi Ready).</param>
/// <param name="ToDate">Ngày cuối của dữ liệu đã sửa của khoảng đó (null khi Ready).</param>
/// <param name="Attempt">Số lần thử lớn nhất của các job còn mở (0 khi Ready).</param>
/// <param name="UpdatedAt">Thời điểm hoạt động gần nhất của job (Ready: lúc job cuối hoàn tất, null nếu chưa từng có).</param>
public sealed record DerivedDataStatusResponse(string State, DateOnly? FromDate, DateOnly? ToDate, int Attempt, DateTimeOffset? UpdatedAt)
{
    public const string Ready = "Ready", Updating = "Updating", Failed = "Failed";
}
