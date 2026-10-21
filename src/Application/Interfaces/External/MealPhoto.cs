namespace Application;

/// <summary>Ảnh bữa ăn đã đọc vào bộ nhớ; chỉ sống trong request, không lưu đâu cả.</summary>
public sealed record MealPhoto(byte[] Data)
{
    /// <summary>image/jpeg | image/png | image/webp theo byte đầu file (không tin Content-Type client gửi); null nếu không nhận ra.</summary>
    public string? MediaType => ImageSniffer.Detect(Data);
}
