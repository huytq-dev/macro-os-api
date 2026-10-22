namespace Application;

/// <summary>
/// Một resolver cho TDEE dùng ở Setup → Preview → Review → Create (cùng nguồn dữ liệu), thứ tự "expenditure hiện tại trước":
/// 1. (Static Expenditure Mode: CHƯA có trong mô hình dữ liệu hiện tại, xem tài liệu thiết kế; bỏ qua)
/// 2. Expenditure động đang Updating → TDEE đã học hiện tại.
/// 3. Đã từng học nhưng hiện Holding/vừa reset → giá trị expenditure hợp lệ gần nhất (không phải công thức).
/// 4. Chưa có ước tính đã học → TDEE khởi điểm user nhập (nếu có). Manual Initial ≠ Static Mode: không bao giờ ghi đè TDEE đã học.
/// 5. Còn lại → công thức hồ sơ (Cunningham/Mifflin).
/// "Đã từng học" = có ít nhất một snapshot Updating của user; snapshot Calibrating chỉ là giá trị khởi tạo (công thức/nhập tay) nên không tính là đã học, trừ khi trước nó đã có snapshot Updating (reset giữ TDEE đã học làm khởi điểm).
/// </summary>
public static class InitialTdeeResolver
{
    public static async Task<ResolvedTdee> ResolveAsync(
        IUnitOfWork uow, Guid userId, UserProfile profile, decimal weightKg, int? manualInitialKcal, DateOnly today, AlgorithmOptions opt, CancellationToken ct)
    {
        // today = ngày LOCAL của user. Công thức chỉ BẮT BUỘC khi nguồn là Formula; thiếu câu trả lời thì formula = null (TDEE đã học/nhập tay vẫn dùng được)
        var bmrSex = await uow.Repository<UserExpenditureSettings>().Query().Where(s => s.UserId == userId).Select(s => s.BmrSex).FirstOrDefaultAsync(ct);
        var formula = OnboardingCalculator.TryEstimateTdee(profile, weightKg, today, bmrSex, out var missingInputs);
        var snaps = uow.Repository<ExpenditureSnapshot>().Query().Where(s => s.UserId == userId);
        var latest = await snaps.OrderByDescending(s => s.SnapshotDate).Select(s => new { s.Status, s.EstimatedTdeeKcal }).FirstOrDefaultAsync(ct);

        int tdee;
        string source;
        if (latest is { Status: ExpenditureStatus.Updating })
        {
            (tdee, source) = ((int)Math.Round(latest.EstimatedTdeeKcal), TdeeSources.LearnedUpdating);
        }
        else if (latest is not null && await snaps.AnyAsync(s => s.Status == ExpenditureStatus.Updating, ct))
        {
            (tdee, source) = ((int)Math.Round(latest.EstimatedTdeeKcal), TdeeSources.LearnedHolding);
        }
        else if (manualInitialKcal is { } manual)
        {
            (tdee, source) = (manual, TdeeSources.ManualInitial);
        }
        else
        {
            if (formula is null) throw new BadRequestException(ExceptionMessages.InitialEstimateNeedsAnswers(string.Join(", ", missingInputs)));
            (tdee, source) = (formula.TdeeKcal, TdeeSources.Formula);
        }

        var stale = (await DerivedDataJobs.StatusAsync(uow, userId, opt, ct)).State != DerivedDataStatusResponse.Ready;
        return new ResolvedTdee(tdee, source, stale, formula);
    }
}
