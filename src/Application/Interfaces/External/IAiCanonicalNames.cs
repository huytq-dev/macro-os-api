namespace Application;

/// <summary>Danh sách tên chuẩn (CanonicalFood.Name, tiếng Anh) đang hoạt động, sắp xếp cố định, có đệm bộ nhớ vài phút; đưa vào đầu system prompt của AI Plate.</summary>
public interface IAiCanonicalNames
{
    Task<IReadOnlyList<string>> GetAsync(CancellationToken ct);
}
