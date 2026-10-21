namespace Application;

/// <summary>WP3 / Select All: xoá (soft) nhiều entry của mình; trả số entry đã xoá. Tổng ngày và expenditure tính lại một lần.</summary>
public sealed record BulkDeleteEntriesCommand(List<Guid> Ids) : IRequest<int>;
