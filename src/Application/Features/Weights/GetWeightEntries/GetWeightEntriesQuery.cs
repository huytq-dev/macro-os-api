namespace Application;

/// <summary>UC-E03: danh sách cân (mới nhất trước) trong khoảng; mặc định 90 ngày gần nhất.</summary>
public sealed record GetWeightEntriesQuery(DateOnly? From, DateOnly? To) : IRequest<WeightEntriesResponse>;
