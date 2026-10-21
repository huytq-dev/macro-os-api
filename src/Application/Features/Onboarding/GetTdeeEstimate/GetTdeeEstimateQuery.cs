namespace Application;

/// <summary>TDEE khởi điểm từ đáp án Basics đã lưu (màn "2380 kcal, otherwise known as expenditure").</summary>
public sealed record GetTdeeEstimateQuery : IRequest<TdeeEstimateResponse>;
