namespace Contract;

public sealed class CreateEventReviewRequest
{
    public int Rating { get; init; }
    public string? Content { get; init; }
}
