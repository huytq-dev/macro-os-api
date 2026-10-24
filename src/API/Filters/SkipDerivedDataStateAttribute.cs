namespace API;

/// <summary>
/// POST chỉ để TÍNH (xem trước, dinh dưỡng plate chưa log, tra nguyên liệu AI): không ghi dữ liệu nguồn nên <see cref="DerivedDataStateFilter"/> không gắn
/// X-Derived-Data-State (bớt một truy vấn tới DB mỗi lần gọi). Danh sách phải KHỚP <c>READ_ONLY_POSTS</c> của FE (<c>FE/src/lib/axios.ts</c>): FE bỏ qua header ở đúng các
/// đường đó; đánh dấu một đường FE chưa bỏ qua thì FE coi "thiếu header" là phải đọc lại status (thêm một request). Kiểm tra đối chiếu: <c>tests/DerivedDataRepro</c> mục 9e.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class SkipDerivedDataStateAttribute : Attribute;
