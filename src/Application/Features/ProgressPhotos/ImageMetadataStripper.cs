namespace Application;

/// <summary>
/// Bỏ metadata riêng tư (EXIF kèm GPS, XMP, IPTC, ghi chú) khỏi ảnh TRƯỚC khi lưu, không giải mã/nén lại ảnh (giữ nguyên chất lượng).
/// JPEG: giữ hướng xoay (Orientation) bằng một khối EXIF tối thiểu chỉ chứa tag đó, để ảnh chụp dọc không bị nằm ngang.
/// PNG: bỏ eXIf/tEXt/zTXt/iTXt/tIME. WebP: bỏ chunk EXIF/XMP và xoá cờ tương ứng ở VP8X. Hàm thuần; ảnh hỏng → <see cref="BadRequestException"/>.
/// </summary>
public static class ImageMetadataStripper
{
    public static byte[] Strip(byte[] data, string mediaType) => mediaType switch
    {
        "image/jpeg" => StripJpeg(data),
        "image/png" => StripPng(data),
        "image/webp" => StripWebp(data),
        _ => throw new BadRequestException(ExceptionMessages.ImageTypeNotSupported),
    };

    private static BadRequestException Corrupt() => new("Ảnh bị hỏng hoặc không đọc được.");

    // ---- JPEG ----

    private static byte[] StripJpeg(byte[] d)
    {
        if (d.Length < 4 || d[0] != 0xFF || d[1] != 0xD8) throw Corrupt();

        var segments = new List<byte[]>();   // các đoạn giữ lại, theo thứ tự
        byte[]? tail = null;                  // từ SOS tới hết file (dữ liệu ảnh), chép nguyên
        var orientation = 0;
        var i = 2;
        while (i < d.Length)
        {
            if (d[i] != 0xFF || i + 1 >= d.Length) throw Corrupt();
            var marker = d[i + 1];
            if (marker == 0xFF) { i++; continue; }                          // byte đệm
            if (marker == 0xD9) { tail = [0xFF, 0xD9]; break; }              // EOI trước SOS (hiếm)
            if (marker == 0x01 || marker is >= 0xD0 and <= 0xD7) { segments.Add([0xFF, marker]); i += 2; continue; }

            if (i + 4 > d.Length) throw Corrupt();
            var len = (d[i + 2] << 8) | d[i + 3];
            if (len < 2 || i + 2 + len > d.Length) throw Corrupt();

            if (marker == 0xDA) { tail = d[i..]; break; }                    // SOS: phần còn lại là dữ liệu ảnh

            // APP1 (EXIF/XMP), APP3–APP13 (IPTC, Photoshop…), APP15 và COM bị bỏ; còn lại (JFIF, ICC, Adobe, bảng, SOF) giữ
            var drop = marker is 0xE1 or (>= 0xE3 and <= 0xED) or 0xEF or 0xFE;
            if (marker == 0xE1 && orientation == 0) orientation = ReadExifOrientation(d, i + 4, len - 2);
            if (!drop) segments.Add(d[i..(i + 2 + len)]);
            i += 2 + len;
        }
        if (tail is null) throw Corrupt();

        using var output = new MemoryStream(d.Length);
        output.Write([0xFF, 0xD8]);
        var insertAt = segments.Count > 0 && segments[0][1] == 0xE0 ? 1 : 0;   // sau JFIF nếu có
        for (var s = 0; s < segments.Count; s++)
        {
            if (s == insertAt && orientation is >= 2 and <= 8) output.Write(OrientationOnlyExif(orientation));
            output.Write(segments[s]);
        }
        if (insertAt >= segments.Count && orientation is >= 2 and <= 8) output.Write(OrientationOnlyExif(orientation));
        output.Write(tail);
        return output.ToArray();
    }

    /// <summary>Tag Orientation (0x0112) trong khối APP1 "Exif\0\0"; 0 nếu không có/không đọc được.</summary>
    private static int ReadExifOrientation(byte[] d, int start, int length)
    {
        if (length < 14 || !d.AsSpan(start, 6).SequenceEqual("Exif\0\0"u8)) return 0;
        var t = start + 6;          // đầu khối TIFF
        var tiffLen = length - 6;
        var little = d[t] == 'I' && d[t + 1] == 'I';
        if (!little && !(d[t] == 'M' && d[t + 1] == 'M')) return 0;

        int U16(int o) => o < 0 || o + 2 > tiffLen ? -1 : little ? d[t + o] | d[t + o + 1] << 8 : d[t + o] << 8 | d[t + o + 1];
        long U32(int o) => o < 0 || o + 4 > tiffLen ? -1 : little
            ? d[t + o] | (long)d[t + o + 1] << 8 | (long)d[t + o + 2] << 16 | (long)d[t + o + 3] << 24
            : (long)d[t + o] << 24 | (long)d[t + o + 1] << 16 | (long)d[t + o + 2] << 8 | d[t + o + 3];

        var ifd = U32(4);
        if (ifd < 8 || ifd > tiffLen) return 0;
        var count = U16((int)ifd);
        for (var k = 0; k < count; k++)
        {
            var entry = (int)ifd + 2 + 12 * k;
            if (U16(entry) == 0x0112 && U16(entry + 2) == 3) return U16(entry + 8);
        }
        return 0;
    }

    /// <summary>APP1 EXIF big-endian chỉ có một tag Orientation (SHORT) — không GPS, không thời gian, không máy chụp.</summary>
    private static byte[] OrientationOnlyExif(int orientation) =>
    [
        0xFF, 0xE1, 0x00, 0x22,                                  // APP1, độ dài 34
        (byte)'E', (byte)'x', (byte)'i', (byte)'f', 0, 0,
        (byte)'M', (byte)'M', 0x00, 0x2A, 0, 0, 0, 8,            // TIFF header, IFD0 ở offset 8
        0x00, 0x01,                                              // 1 entry
        0x01, 0x12, 0x00, 0x03, 0, 0, 0, 1, 0x00, (byte)orientation, 0, 0,
        0, 0, 0, 0,                                              // không có IFD kế tiếp
    ];

    // ---- PNG ----

    private static readonly HashSet<string> PngDropped = ["eXIf", "tEXt", "zTXt", "iTXt", "tIME"];

    private static byte[] StripPng(byte[] d)
    {
        if (d.Length < 12 || ImageSniffer.Detect(d) != "image/png") throw Corrupt();
        using var output = new MemoryStream(d.Length);
        output.Write(d, 0, 8);
        var i = 8;
        var sawEnd = false;
        while (i + 12 <= d.Length)
        {
            var len = (int)((uint)d[i] << 24 | (uint)d[i + 1] << 16 | (uint)d[i + 2] << 8 | d[i + 3]);
            if (len < 0 || i + 12 + len > d.Length) throw Corrupt();
            var type = System.Text.Encoding.ASCII.GetString(d, i + 4, 4);
            if (!PngDropped.Contains(type)) output.Write(d, i, 12 + len);
            i += 12 + len;
            if (type == "IEND") { sawEnd = true; break; }
        }
        return sawEnd ? output.ToArray() : throw Corrupt();
    }

    // ---- WebP ----

    private static byte[] StripWebp(byte[] d)
    {
        if (d.Length < 20 || ImageSniffer.Detect(d) != "image/webp") throw Corrupt();
        using var body = new MemoryStream(d.Length);
        var i = 12;
        while (i + 8 <= d.Length)
        {
            var size = d[i + 4] | d[i + 5] << 8 | d[i + 6] << 16 | d[i + 7] << 24;
            if (size < 0) throw Corrupt();
            var end = Math.Min(i + 8 + size + (size & 1), d.Length);   // chunk lẻ có 1 byte đệm
            if (i + 8 + size > d.Length) throw Corrupt();
            var fourCc = System.Text.Encoding.ASCII.GetString(d, i, 4);
            if (fourCc is not ("EXIF" or "XMP "))
            {
                var chunk = d[i..end];
                if (fourCc == "VP8X" && chunk.Length > 8) chunk[8] &= 0xF3; // bỏ cờ EXIF (0x08) và XMP (0x04)
                body.Write(chunk);
            }
            i = end;
        }

        var riffSize = (int)body.Length + 4;
        using var output = new MemoryStream(riffSize + 8);
        output.Write("RIFF"u8);
        output.Write([(byte)riffSize, (byte)(riffSize >> 8), (byte)(riffSize >> 16), (byte)(riffSize >> 24)]);
        output.Write("WEBP"u8);
        body.WriteTo(output);
        return output.ToArray();
    }
}
