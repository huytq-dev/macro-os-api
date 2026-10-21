namespace Application;

/// <summary>
/// UC-E04: % mỡ cơ thể mới nhất trước; lọc theo phương pháp đo. Không có From/To: mặc định 7 lần gần nhất, tối đa 100 (Limit).
/// Có From và/hoặc To: trả MỌI dòng có % mỡ trong [From, To] (gồm cả hai đầu, bỏ qua Limit), MeasuredDate rồi CreatedAt giảm dần; LatestPercent vẫn là lần mới nhất của phương pháp đó (cả ngoài khoảng).
/// </summary>
public sealed record GetBodyFatQuery(MeasurementMethod? Method, int? Limit, DateOnly? From = null, DateOnly? To = null) : IRequest<BodyFatResponse>;
