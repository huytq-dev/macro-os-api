namespace Application;

/// <summary>Ghi % mỡ Visual của một ngày (Date null = hôm nay theo múi giờ user, không được ở tương lai); ngày đã có thì ghi đè (mỗi ngày một dòng).</summary>
public sealed record UpsertVisualBodyFatCommand(DateOnly? Date, decimal Percent) : IRequest<Guid>;
