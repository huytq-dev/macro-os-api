namespace Application;

/// <summary>Trang Habits (Weigh-In): ngày có cân/% mỡ của một tháng (lịch) hoặc cả năm (heatmap) + streak. Year mặc định năm hiện tại (theo múi giờ của user), Month null = cả năm.</summary>
public sealed record GetWeighInHabitQuery(int? Year, int? Month) : IRequest<WeighInHabitResponse>;
