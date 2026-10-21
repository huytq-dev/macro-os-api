namespace Application;

/// <summary>
/// So sánh hai ngày: mỗi chỉ số có ở ít nhất một ngày → giá trị A, giá trị B, thay đổi (B − A, null nếu thiếu một bên), xu hướng (Up | Down | Same) và đánh giá theo mục tiêu của user
/// (program đang chạy: WeightLoss → giảm là Good; WeightGain/MuscleGain → tăng là Good; còn lại Neutral). % mỡ Visual: giảm luôn là Good. Nhóm WeightAndBodyFat gồm cân, trend và % mỡ Visual.
/// </summary>
public sealed record GetFullBodyCompareQuery(DateOnly? DateA, DateOnly? DateB) : IRequest<FullBodyCompareResponse>;
