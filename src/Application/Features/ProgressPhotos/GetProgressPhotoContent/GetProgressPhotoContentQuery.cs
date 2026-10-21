namespace Application;

/// <summary>Nội dung ảnh cho URL BE đã ký: chữ ký đúng + chưa hết hạn mới trả; sai/hết hạn/không tồn tại đều 404 (không lộ ảnh có tồn tại hay không).</summary>
public sealed record GetProgressPhotoContentQuery(Guid Id, long Exp, string? Sig) : IRequest<ProgressPhotoContent>, ISensitiveRequest;
