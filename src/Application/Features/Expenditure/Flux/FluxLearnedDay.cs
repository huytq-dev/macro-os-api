namespace Application;

/// <summary>Một ngày của chuỗi TDEE ĐÃ HỌC (snapshot lưu của production, ngày thiếu snapshot giữ TDEE trước + Holding như <c>ExpenditureSeries.Daily</c>). Đầu vào của Flux, không bao giờ bị Flux sửa.</summary>
public sealed record FluxLearnedDay(DateOnly Date, decimal TdeeKcal, ExpenditureStatus Status);
