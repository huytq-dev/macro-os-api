using System.Diagnostics;

namespace Application;

/// <summary>
/// Dựng lại dữ liệu dẫn xuất NGAY sau khi sửa dữ liệu nguồn (log ăn, cân, ngày chưa đủ), trước khi API trả về, để refetch thấy số mới:
/// transaction + khoá user → làm mới aggregate dinh dưỡng của các ngày sửa → dựng snapshot từ ngày sửa với dừng sớm khi hội tụ và trần số dòng.
/// Trần số dòng (<see cref="AlgorithmOptions.DerivedSyncRowBudget"/>) chỉ là NGÂN SÁCH HIỆU NĂNG của request, không phải giả định "cửa sổ N ngày là đủ":
/// MỌI phần chưa dựng (chạm trần, lỗi, không lấy được khoá, ngân sách 0) luôn thành job bền (<see cref="DerivedDataJobs"/>) trong cùng transaction hoặc ngay sau đó;
/// dữ liệu nguồn đã lưu từ trước nên không bao giờ mất.
/// <para>
/// Vì sao dừng khi hội tụ là ĐỦ (đúng, không phải ước lượng): snapshot ngày d chỉ phụ thuộc (a) dữ liệu nguồn trong cửa sổ W ngày kết thúc ở d (kcal hợp lệ, trend các lần cân),
/// (b) 7 ngày gần nhất cho Updating/Holding, (c) TDEE ngày d−1 qua EMA <c>x[d] = (1−β)·x[d−1] + β·raw[d]</c>. Từ ngày <c>sửa + W</c> mọi cửa sổ không còn chứa ngày vừa sửa,
/// nên nếu snapshot tính lại của W ngày liên tiếp trùng snapshot đang lưu (mọi cột giá trị, độ chính xác lưu trữ 2 chữ số) thì trạng thái EMA và mọi đầu vào của các cửa sổ kế tiếp
/// đều trùng bản đã lưu ⇒ quy nạp: các ngày sau cũng trùng. Sai lệch dưới độ chính xác lưu trữ (&lt; 0.005 kcal/ngày) co theo (1−β)ⁿ nên không tích luỹ. Hội tụ KHÔNG dựa vào số ngày cố định:
/// đo trên 730 ngày dữ liệu cần ~130–150 ngày cho sửa cân 2 kg (đuôi EMA × đuôi trend), và một dòng chưa hội tụ trong ngân sách thì phần còn lại đi qua job bền.
/// </para>
/// </summary>
public static class DerivedDataSync
{
    public static string LockResource(Guid userId) => $"macroos:derived:{userId:N}";

    /// <param name="fromDate">Ngày sớm nhất cần dựng lại snapshot.</param>
    /// <param name="editedTo">Ngày muộn nhất của dữ liệu đã sửa (cửa sổ tính còn chứa nó tới editedTo + W − 1; hội tụ chỉ xét sau mốc này).</param>
    /// <param name="nutritionDates">Các ngày cần làm mới aggregate dinh dưỡng; null = không.</param>
    /// <param name="bounded">false = dựng hết khoảng đến hôm nay không giới hạn (job đêm).</param>
    /// <returns>true = dữ liệu dẫn xuất đã mới hoàn toàn khi trả về; false = còn job nền (FE thấy Updating).</returns>
    public static async Task<bool> RunAsync(
        IUnitOfWork uow, Guid userId, DateOnly fromDate, DateOnly editedTo, IReadOnlyCollection<DateOnly>? nutritionDates, RecalculationCause cause,
        AlgorithmOptions opt, TimeProvider tp, CancellationToken ct, bool bounded = true)
    {
        var watch = Stopwatch.StartNew();
        var rows = 0;
        string outcome;
        if (bounded && opt.DerivedSyncRowBudget <= 0)
        {
            await DerivedDataJobs.EnqueueAsync(uow, userId, fromDate, editedTo, cause, null, ct);
            uow.Derived.Wake();
            uow.Derived.ReportSync(cause.ToString(), watch.ElapsedMilliseconds, 0, "Queued");
            return false;
        }

        try
        {
            await uow.BeginTransactionAsync(ct);
            if (!await uow.TryAcquireTransactionLockAsync(LockResource(userId), opt.DerivedLockWaitMs, ct))
            {
                await uow.RollbackTransactionAsync(ct);
                await DerivedDataJobs.EnqueueAsync(uow, userId, fromDate, editedTo, cause, null, ct);
                uow.Derived.Wake();
                uow.Derived.ReportSync(cause.ToString(), watch.ElapsedMilliseconds, 0, "Queued");
                return false;
            }

            if (nutritionDates is { Count: > 0 }) await NutritionAggregates.RefreshAsync(uow, userId, nutritionDates, tp, ct);
            var limits = bounded ? new ExpenditureRebuildLimits(editedTo.AddDays(opt.ExpenditureWindowDays), opt.DerivedSyncRowBudget) : null;
            // Ngày local của user (múi giờ hồ sơ): dựng tới hôm nay LOCAL và hôm nay chưa log hợp lệ không bị tính là thiếu
            var localToday = (await UserLocale.GetAsync(uow, userId, tp, ct)).Today;
            var result = await ExpenditureRebuilder.RebuildAsync(uow, userId, fromDate, localToday, opt, ct, limits, localToday);
            rows = result.RowsWritten;
            if (result.ResumeFrom is { } resume) await DerivedDataJobs.EnqueueAsync(uow, userId, resume, resume, cause, null, ct);
            await uow.SaveChangesAsync(ct);
            await uow.CommitTransactionAsync(ct);
            outcome = result.ResumeFrom is null ? "Completed" : "Queued";
            if (result.ResumeFrom is not null) uow.Derived.Wake();
            uow.Derived.ReportSync(cause.ToString(), watch.ElapsedMilliseconds, rows, outcome);
            return result.ResumeFrom is null;
        }
        catch (Exception ex)
        {
            // Nguồn đã lưu trước khi vào đây: lỗi/huỷ request chỉ làm dẫn xuất trễ, không mất dữ liệu. Rollback rồi xếp job bền (không dùng ct của request: có thể đã bị huỷ).
            try { await uow.RollbackTransactionAsync(CancellationToken.None); } catch { /* transaction có thể đã đóng */ }
            uow.DiscardChanges();
            await DerivedDataJobs.EnqueueAsync(uow, userId, fromDate, editedTo, cause, ex.Message, CancellationToken.None);
            uow.Derived.Wake();
            uow.Derived.ReportSync(cause.ToString(), watch.ElapsedMilliseconds, rows, "Failed");
            return false;
        }
    }
}
