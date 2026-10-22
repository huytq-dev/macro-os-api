namespace Domain;

// Lịch sử Static/Dynamic của user: mỗi dòng có hiệu lực từ EffectiveFrom (ngày LOCAL của user) tới ngày trước dòng kế tiếp; không có dòng = Dynamic.
// Chuyển nhiều lần trong cùng một ngày = ghi đè dòng của ngày đó (unique UserId+EffectiveFrom), không bao giờ có hai khoảng chồng nhau.
public class ExpenditureModePeriod : EntityBase<Guid>
{
    public Guid UserId { get; set; }
    public DateOnly EffectiveFrom { get; set; }
    public ExpenditureMode Mode { get; set; }
    public int? StaticTdeeKcal { get; set; }    // bắt buộc > 0 khi Static, null khi Dynamic

    public User User { get; set; } = null!;
}
