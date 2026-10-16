using Microsoft.AspNetCore.Http;

namespace Application;

public interface ICloudinaryService
{
    Task<string> UploadPhotoAsync(IFormFile file);
    Task DeletePhotoAsync(string publicId);
}
