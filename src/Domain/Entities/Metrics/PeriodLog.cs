namespace Domain;

public class PeriodLog : EntityBase<Guid>
{
    public Guid UserId { get; set; }

    public DateOnly StartDate { get; set; }

    // null nếu đang trong kỳ
    public DateOnly? EndDate { get; set; }

    // Tính từ StartDate kỳ này đến StartDate kỳ trước (null nếu là kỳ đầu tiên)
    public int? CycleLengthDays { get; set; }

    // Computed từ EndDate - StartDate
    public int? PeriodLengthDays { get; set; }

    public string? Symptoms { get; set; }
    public string? Notes { get; set; }

    public User User { get; set; } = null!;
}
