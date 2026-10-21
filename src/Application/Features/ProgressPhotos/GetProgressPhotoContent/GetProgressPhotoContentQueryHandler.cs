namespace Application;

public sealed class GetProgressPhotoContentQueryHandler(IUnitOfWork _uow, TimeProvider _timeProvider, IPrivateImageStore _store, IOptions<JwtOptions> _jwt)
    : IRequestHandler<GetProgressPhotoContentQuery, ProgressPhotoContent>
{
    public async Task<ProgressPhotoContent> Handle(GetProgressPhotoContentQuery request, CancellationToken ct)
    {
        var row = await _uow.Repository<ProgressPhoto>().FindAsync(p => p.Id == request.Id, ct);
        if (row is null || !ProgressPhotoUrlSigner.Verify(_jwt.Value.SecretKey, row.Id, row.UserId, request.Exp, request.Sig, _timeProvider.GetUtcNow()))
            throw new NotFoundException(ExceptionMessages.NotFound("ProgressPhoto", request.Id));

        var data = await _store.DownloadAsync(row.StorageKey, ct)
            ?? throw new NotFoundException(ExceptionMessages.NotFound("ProgressPhoto", request.Id));
        return new ProgressPhotoContent(data, row.ContentType);
    }
}
