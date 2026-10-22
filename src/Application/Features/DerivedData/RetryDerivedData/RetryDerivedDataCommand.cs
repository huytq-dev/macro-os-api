namespace Application;

/// <summary>Thử lại các job dựng lại đã hết lượt thử (Failed): đặt về Pending với số lần thử 0 và đánh thức worker. Không có job Failed thì không làm gì. Trả trạng thái mới.</summary>
public sealed record RetryDerivedDataCommand : IRequest<DerivedDataStatusResponse>;
