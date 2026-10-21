namespace Application;

/// <summary>Tuần (Thứ 2 → Chủ nhật) chứa ngày `Date`, mặc định hôm nay: tổng macro + target hiệu lực của từng ngày, một request cho dashboard.</summary>
public sealed record GetWeekLogQuery(DateOnly? Date) : IRequest<WeekLogResponse>;
