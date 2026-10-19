using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

namespace Infrastructure;

/// <summary>
/// Kho ảnh riêng tư trên Cloudinary: upload với <c>type = authenticated</c> nên ảnh KHÔNG có URL công khai; BE tự tải về bằng URL ký phía server
/// rồi trả cho client sau khi kiểm quyền. (URL ký của Cloudinary không tự hết hạn — token hết hạn là tính năng trả phí — nên thời hạn do BE ký, xem ProgressPhotoUrlSigner.)
/// </summary>
[RegisterService(typeof(IPrivateImageStore))]
public sealed class CloudinaryPrivateImageStore(IOptions<CloudinaryOptions> options, IHttpClientFactory _http, ILogger<CloudinaryPrivateImageStore> _logger) : IPrivateImageStore
{
    private const string DeliveryType = "authenticated";
    private readonly Cloudinary _cloudinary = new(new Account(options.Value.CloudName, options.Value.ApiKey, options.Value.ApiSecret));

    public async Task UploadAsync(string storageKey, byte[] data, CancellationToken ct)
    {
        try
        {
            using var stream = new MemoryStream(data);
            var result = await _cloudinary.UploadAsync(new ImageUploadParams
            {
                File = new FileDescription(storageKey, stream),
                PublicId = storageKey,
                Type = DeliveryType,
                Overwrite = true,
                Invalidate = true,
            }, ct);
            if (result.Error is not null)
            {
                _logger.LogWarning("Private image upload failed: {Message}", result.Error.Message);
                throw new ExternalServiceException(ExceptionMessages.ImageSaveFailed);
            }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Private image upload failed");
            throw new ExternalServiceException(ExceptionMessages.ImageSaveFailed);
        }
    }

    public async Task<byte[]?> DownloadAsync(string storageKey, CancellationToken ct)
    {
        // URL ký chỉ dùng ở phía server, không bao giờ trả cho client và không ghi log
        var url = _cloudinary.Api.UrlImgUp.Secure(true).Signed(true).Type(DeliveryType).BuildUrl(storageKey);
        try
        {
            using var response = await _http.CreateClient().GetAsync(url, ct);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Private image download failed: {Status}", (int)response.StatusCode);
                throw new ExternalServiceException(ExceptionMessages.ImageLoadFailed);
            }
            return await response.Content.ReadAsByteArrayAsync(ct);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Private image download failed");
            throw new ExternalServiceException(ExceptionMessages.ImageLoadFailed);
        }
    }

    public async Task DeleteAsync(string storageKey, CancellationToken ct)
    {
        var result = await _cloudinary.DestroyAsync(new DeletionParams(storageKey) { Type = DeliveryType, Invalidate = true });
        if (result.Result is not ("ok" or "not found"))
        {
            _logger.LogWarning("Private image delete failed: {Result}", result.Result);
            throw new ExternalServiceException(ExceptionMessages.ImageDeleteFailed);
        }
    }
}
