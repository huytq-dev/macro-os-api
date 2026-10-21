namespace Application;

public static class ProgressPhotoSupport
{
    public static ProgressPhotoModel ToModel(ProgressPhoto row, string secret, ProgressPhotoOptions opt, DateTimeOffset now)
    {
        var expires = now.AddMinutes(opt.SignedUrlMinutes);
        var exp = expires.ToUnixTimeSeconds();
        return new ProgressPhotoModel(row.Id, row.Angle.ToString(), ProgressPhotoUrlSigner.Url(row.Id, exp, ProgressPhotoUrlSigner.Sign(secret, row.Id, row.UserId, exp)), expires, row.CapturedAt);
    }

    /// <summary>Thời điểm user đồng ý lần đầu (kể cả khi ảnh đã xoá: đồng ý không mất khi xoá ảnh); null nếu chưa từng.</summary>
    public static async Task<DateTimeOffset?> FirstConsentAsync(IUnitOfWork uow, Guid userId, CancellationToken ct)
        => await uow.Repository<ProgressPhoto>().Query().IgnoreQueryFilters()
            .Where(p => p.UserId == userId && p.ConsentGivenAt != null)
            .OrderBy(p => p.ConsentGivenAt)
            .Select(p => p.ConsentGivenAt)
            .FirstOrDefaultAsync(ct);
}
