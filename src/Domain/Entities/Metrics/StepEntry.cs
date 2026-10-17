namespace Domain;

// Tổng số bước 1 ngày (nhập tay / sync Health); chỉ hiển thị, không vào thuật toán expenditure
public class StepEntry : EntityBase<Guid>
{
    public Guid UserId { get; set; }
    public DateOnly LogDate { get; set; }
    public int Steps { get; set; }
    public StepSource Source { get; set; } = StepSource.Manual;

    public User User { get; set; } = null!;
}
