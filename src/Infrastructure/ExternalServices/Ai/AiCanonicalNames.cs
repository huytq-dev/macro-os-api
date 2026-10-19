using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Infrastructure;

/// <summary>
/// Tên chuẩn tiếng Anh của catalog (CanonicalFood đang Active) cho prompt AI Plate. Đệm bộ nhớ vài phút nên mỗi lần analyze không phải đọc DB;
/// seed thêm thực phẩm chuẩn thì tự có mặt sau tối đa <see cref="Ttl"/>. Danh sách sắp xếp cố định để phần đầu prompt không đổi giữa các lần gọi (prompt caching).
/// Bỏ muối/tiêu/bột ngọt: AI không được báo gia vị ≈0 kcal.
/// </summary>
[RegisterService(typeof(IAiCanonicalNames))]
public sealed class AiCanonicalNames(IUnitOfWork _uow, IMemoryCache _cache) : IAiCanonicalNames
{
    private const string CacheKey = "ai:canonical-names";
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);

    public async Task<IReadOnlyList<string>> GetAsync(CancellationToken ct)
    {
        if (_cache.TryGetValue(CacheKey, out IReadOnlyList<string>? cached) && cached is not null) return cached;

        var names = (await _uow.Repository<CanonicalFood>().Query()
                .Where(c => c.Status == CanonicalFoodStatus.Active).Select(c => c.Name).ToListAsync(ct))
            .Where(n => !string.IsNullOrWhiteSpace(n) && !AiPlateNormalizer.IsZeroCalorieSeasoning(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();

        _cache.Set(CacheKey, (IReadOnlyList<string>)names, Ttl);
        return names;
    }
}
