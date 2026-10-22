namespace Application;

/// <summary>Trạng thái dựng lại dữ liệu dẫn xuất của user hiện tại (Ready | Updating | Failed): FE biết số trên màn hình đã khớp dữ liệu nguồn hay còn đang cập nhật.</summary>
public sealed record GetDerivedDataStatusQuery : IRequest<DerivedDataStatusResponse>;
