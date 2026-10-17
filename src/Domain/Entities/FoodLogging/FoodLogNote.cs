namespace Domain;

// Ghi chú của một ngày (nhiều note/ngày); tách khỏi FoodLog để ghi chú không làm ngày thành "đã log"
public class FoodLogNote : EntityBase<Guid>
{
    public Guid UserId { get; set; }
    public DateOnly LogDate { get; set; }
    public string Text { get; set; } = null!;
    public TimeOnly? NotedAt { get; set; }      // giờ gắn với ghi chú, hiện trên timeline

    public User User { get; set; } = null!;
}
