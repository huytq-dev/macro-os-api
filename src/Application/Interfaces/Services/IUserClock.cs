namespace Application;

/// <summary>
/// Đồng hồ nghiệp vụ của user hiện tại: MỘT định nghĩa "hôm nay" theo múi giờ hồ sơ (<see cref="UserLocale"/>). Cache theo request (scoped, một truy vấn hồ sơ);
/// đổi múi giờ giữa phiên có hiệu lực ở request kế tiếp, và ngay trong request nếu handler gọi <see cref="Invalidate"/> sau khi đổi.
/// Ngày nghiệp vụ (log, cân, check-in, program, snapshot, tuổi) LUÔN lấy từ đây; UTC chỉ cho dấu thời gian/audit/token.
/// </summary>
public interface IUserClock
{
    Task<UserLocale> GetAsync(CancellationToken ct);
    void Invalidate();
}
