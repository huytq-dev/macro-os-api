namespace Application;

/// <summary>UC-E07: số bước theo ngày (mới nhất trước); mặc định 30 ngày gần nhất.</summary>
public sealed record GetStepEntriesQuery(DateOnly? From, DateOnly? To) : IRequest<StepEntriesResponse>;
