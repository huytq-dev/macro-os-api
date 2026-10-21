namespace Application;

/// <summary>
/// UC-C08: tra món theo mã vạch. Có sẵn trong DB (món hệ thống/OFF đã quét/của mình) thì trả ngay; chưa có thì hỏi Open Food Facts,
/// lưu thành FoodItem dùng chung (cache cho lần quét sau) rồi trả. OFF không có → 404; OFF lỗi → 502.
/// </summary>
public sealed record GetFoodByBarcodeQuery(string Code) : IRequest<FoodItemResponse>;
