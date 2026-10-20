namespace Application;

/// <summary>
/// Cache trong bộ nhớ cho dữ liệu THAM CHIẾU gần như bất biến (mã chất, món hệ thống + hồ sơ chất của chúng): mỗi lần tra là một vòng đi về mạng tới DB ở xa (~200 ms).
/// Giá trị cache được dùng CHỈ ĐỌC (không gắn vào DbContext). Hết hạn theo TTL; quá số mục tối đa thì xoá hết. Bản Noop (UnitOfWork dựng thủ công trong test) không cache gì.
/// </summary>
public interface IReferenceCache
{
    bool TryGet<T>(string key, out T value);
    void Set<T>(string key, T value, TimeSpan ttl);
}
