namespace Application;

public sealed class GetProgressPhotosQueryHandler(
    IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider, IOptions<ProgressPhotoOptions> _options, IOptions<JwtOptions> _jwt)
    : IRequestHandler<GetProgressPhotosQuery, ProgressPhotosResponse>
{
    public async Task<ProgressPhotosResponse> Handle(GetProgressPhotosQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var date = request.Date ?? (await UserLocale.GetAsync(_uow, userId, _timeProvider, ct)).Today;
        var now = _timeProvider.GetUtcNow();

        var rows = (await _uow.Repository<ProgressPhoto>().Query()
            .Where(p => p.UserId == userId && p.PhotoDate == date)
            .ToListAsync(ct)).OrderBy(r => (int)r.Angle).ToList(); // Front, Side, Back (cột Angle lưu chuỗi nên không sắp theo SQL được)
        var consent = await ProgressPhotoSupport.FirstConsentAsync(_uow, userId, ct);
        var weights = await FullBodyWriter.WeightsAsync(_uow, userId, date, date, ct);
        var hasWeight = weights.TryGetValue(date, out var weight);

        return new ProgressPhotosResponse(date, consent is not null,
            rows.Select(r => ProgressPhotoSupport.ToModel(r, _jwt.Value.SecretKey, _options.Value, now)).ToList(),
            hasWeight ? weight.Scale : null, hasWeight ? weight.Trend : null);
    }
}
