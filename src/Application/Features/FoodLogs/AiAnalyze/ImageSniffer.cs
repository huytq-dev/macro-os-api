using System.Globalization;
using System.Text.Json;

namespace Application;

/// <summary>Nhận loại ảnh theo magic bytes.</summary>
public static class ImageSniffer
{
    private static ReadOnlySpan<byte> PngSignature => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public static string? Detect(ReadOnlySpan<byte> d)
    {
        if (d.Length >= 3 && d[0] == 0xFF && d[1] == 0xD8 && d[2] == 0xFF) return "image/jpeg";
        if (d.Length >= 8 && d[..8].SequenceEqual(PngSignature)) return "image/png";
        if (d.Length >= 12 && d[..4].SequenceEqual("RIFF"u8) && d[8..12].SequenceEqual("WEBP"u8)) return "image/webp";
        return null;
    }
}
