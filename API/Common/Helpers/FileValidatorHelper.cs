namespace API;

public static class FileValidatorHelper
{
    private static readonly string[] PermittedExtensions = [".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp"];
    private static readonly string[] PermittedMimeTypes = ["image/jpeg", "image/png", "image/gif", "image/bmp", "image/webp"];

    public static bool IsValidExtension(IFormFile? file)
    {
        if (file is null) return false;
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        return PermittedExtensions.Contains(extension);
    }

    public static bool IsValidMimeType(IFormFile? file)
    {
        if (file is null) return false;
        return PermittedMimeTypes.Contains(file.ContentType.ToLowerInvariant());
    }

    public static bool IsValidSize(IFormFile? file, long maxBytes)
    {
        if (file is null) return false;
        return file.Length > 0 && file.Length <= maxBytes;
    }
}
