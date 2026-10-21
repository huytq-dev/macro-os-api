namespace Application;

/// <summary>
/// Đổi icon của một món (bảng "Choose Icon"). Món tự tạo của tôi: ghi thẳng vào món; món hệ thống/USDA/OFF: lưu icon riêng của user (UserFoodIcon).
/// IconKey null = bỏ lựa chọn: món hệ thống xoá icon riêng (về icon của món), món của tôi về icon tự gán theo tên (có thể null). Trả iconKey hiệu lực sau khi đổi.
/// </summary>
public sealed record SetFoodIconCommand(
    [property: System.Text.Json.Serialization.JsonIgnore] Guid Id,
    string? IconKey
) : IRequest<FoodIconResponse>;
