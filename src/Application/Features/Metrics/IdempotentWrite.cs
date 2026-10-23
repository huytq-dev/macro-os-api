namespace Application;

/// <summary>
/// Ghi idempotent cho dữ liệu "một giá trị / ngày": hai request đồng thời cùng tạo dòng đầu tiên của một ngày thì unique index làm một bên thua (<see cref="DbUpdateException"/>). Bên thua bỏ phần chưa lưu rồi chạy LẠI đúng một lần:
/// lần chạy lại đọc thấy dòng của bên thắng và CẬP NHẬT nó (không tạo thêm điểm, không lỗi 500). Vẫn lỗi sau lần chạy lại ⇒ <see cref="ConflictException"/> (409), không âm thầm bỏ ghi.
/// <paramref name="attempt"/> phải tự đọc lại dữ liệu (không giữ entity của lần trước) và gọi SaveChanges.
/// </summary>
public static class IdempotentWrite
{
    public static async Task<T> RunAsync<T>(Func<Task<T>> attempt, Action discardChanges)
    {
        try { return await attempt(); }
        catch (DbUpdateException)
        {
            discardChanges();
            try { return await attempt(); }
            catch (DbUpdateException) { discardChanges(); throw new ConflictException(ExceptionMessages.ConcurrentWriteConflict); }
        }
    }

    public static Task RunAsync(Func<Task> attempt, Action discardChanges)
        => RunAsync(async () => { await attempt(); return 0; }, discardChanges);
}
