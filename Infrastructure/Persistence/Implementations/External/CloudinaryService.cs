using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;

namespace Infrastructure;

[RegisterService(typeof(ICloudinaryService))]
public class CloudinaryService : ICloudinaryService
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryService(IAppConfiguration appConfiguration)
    {
        var cloudinary = appConfiguration.GetCloudinaryOptions();
        var account = new Account(
            cloudinary.CloudName,
            cloudinary.ApiKey,
            cloudinary.ApiSecret
        );

        _cloudinary = new Cloudinary(account);
    }

    public async Task<string> UploadPhotoAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
            throw new ArgumentException("File cannot be empty");

        var uploadResult = new ImageUploadResult();

        using (var stream = file.OpenReadStream())
        {
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Transformation = new Transformation().Height(500).Width(500).Crop("fill").Gravity("face"),
                Folder = "hanh-trang-so-avatars"
            };

            uploadResult = await _cloudinary.UploadAsync(uploadParams);
        }

        if (uploadResult.Error != null)
            throw new Exception($"Cloudinary Error: {uploadResult.Error.Message}");

        return uploadResult.SecureUrl.AbsoluteUri;
    }

    public async Task DeletePhotoAsync(string publicId)
    {
        if (string.IsNullOrWhiteSpace(publicId))
            return;

        var deletionParams = new DeletionParams(publicId);
        var deletionResult = await _cloudinary.DestroyAsync(deletionParams);

        if (deletionResult.Result != "ok" && deletionResult.Result != "not found")
            throw new Exception($"Cloudinary deletion failed: {deletionResult.Result}");
    }
}
