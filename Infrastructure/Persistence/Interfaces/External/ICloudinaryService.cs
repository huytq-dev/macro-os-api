using Microsoft.AspNetCore.Http;

namespace Infrastructure;

public interface ICloudinaryService
{
    Task<string> UploadPhotoAsync(IFormFile file);
    Task DeletePhotoAsync(string publicId);
}
