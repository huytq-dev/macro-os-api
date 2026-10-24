namespace Application;

/// <summary>Chỉ trạng thái dựng lại (Ready | Updating | Failed) của user hiện tại, một truy vấn: dùng cho header X-Derived-Data-State của lệnh ghi.</summary>
public sealed record GetDerivedDataStateQuery : IRequest<string>;

public sealed class GetDerivedDataStateQueryHandler(IUnitOfWork _uow, IUserContext _userContext, IOptions<AlgorithmOptions> _options)
    : IRequestHandler<GetDerivedDataStateQuery, string>
{
    public Task<string> Handle(GetDerivedDataStateQuery request, CancellationToken ct)
        => DerivedDataJobs.OpenStateAsync(_uow, _userContext.UserId, _options.Value, ct);
}
