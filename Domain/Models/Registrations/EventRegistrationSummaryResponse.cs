namespace Contract;

public sealed class EventRegistrationSummaryResponse
{
    public Guid Id { get; init; }
    public Guid EventId { get; init; }
    public required string EventTitle { get; init; }
    public string? EventThumbnailUrl { get; init; }
    public DateTimeOffset StartTime { get; init; }
    public DateTimeOffset EndTime { get; init; }
    public string LocationName { get; init; } = null!;
    public DateTimeOffset RegisteredAt { get; init; }
    public int Status { get; init; }
    public string TicketCode { get; init; } = null!;
    public string? QrCodeUrl { get; init; }
    public decimal Price { get; init; }
}
