namespace Domain;

// Chống double-submit cho mutation quan trọng (commit plate, start/finish session, activate program)
public class IdempotencyRecord : EntityBase<Guid>
{
    public Guid UserId { get; set; }
    public string Key { get; set; } = null!;
    public string Endpoint { get; set; } = null!;
    public string RequestHash { get; set; } = null!;
    public int? ResponseStatus { get; set; }
    public string? ResponseBody { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}
