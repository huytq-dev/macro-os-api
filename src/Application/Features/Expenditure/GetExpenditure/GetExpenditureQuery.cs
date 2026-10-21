namespace Application;

/// <summary>UC-F05: chuỗi TDEE (mặc định 90 ngày) + status + lý do Holding.</summary>
public sealed record GetExpenditureQuery(DateOnly? From, DateOnly? To) : IRequest<ExpenditureResponse>;
