namespace Application;

public sealed class GetDerivedDataStatusQueryHandler(IUnitOfWork _uow, IUserContext _userContext, IOptions<AlgorithmOptions> _options)
    : IRequestHandler<GetDerivedDataStatusQuery, DerivedDataStatusResponse>
{
    public Task<DerivedDataStatusResponse> Handle(GetDerivedDataStatusQuery request, CancellationToken ct)
        => DerivedDataJobs.StatusAsync(_uow, _userContext.UserId, _options.Value, ct);
}
