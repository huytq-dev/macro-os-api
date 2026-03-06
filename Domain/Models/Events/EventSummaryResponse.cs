namespace Contract;

public sealed class EventSummaryResponse
{
    public Guid Id { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public string? ThumbnailUrl { get; init; }
    public DateTimeOffset StartTime { get; init; }
    public DateTimeOffset EndTime { get; init; }
    public int MaxParticipants { get; init; }
    public int RegisteredCount { get; init; }
    public int Status { get; init; }
    public bool IsPrivate { get; init; }
    public Guid CategoryId { get; init; }
    public string CategoryName { get; init; } = null!;
    public Guid LocationId { get; init; }
    public string LocationName { get; init; } = null!;
    public Guid OrganizerId { get; init; }
    public Guid? ClubId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
