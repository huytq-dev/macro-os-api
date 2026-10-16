namespace Domain;

// 1 lần commit plate = 1 batch; IdempotencyKey chống nhân đôi khi retry
public class FoodLogBatch : EntityBase<Guid>
{
    public Guid UserId { get; set; }
    public string IdempotencyKey { get; set; } = null!;
    public string? Source { get; set; }             // plate, copy, template, scan
    public string? MetadataJson { get; set; }

    public User User { get; set; } = null!;
}
