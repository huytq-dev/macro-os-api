namespace Application;

/// <summary>UC-G02: tuần qua + đề xuất target tuần tới + lý do. Xem được bất kỳ lúc nào; IsDue cho biết đã tới ngày.</summary>
public sealed record GetCheckInPreviewQuery : IRequest<CheckInPreviewResponse>;
