namespace Application;

/// <summary>Đọc/khởi tạo lười <see cref="UserExpenditureSettings"/> và dựng response. Khởi tạo chỉ tạo dòng mặc định (toàn NULL): không bao giờ ghi đè TDEE nhập tay, TDEE đã học hay dòng đã có.</summary>
public static class ExpenditureSettingsSupport
{
    /// <summary>Lấy dòng settings (tracked); chưa có thì tạo dòng mặc định. Hai request đồng thời: unique(UserId) làm một bên thua, bên đó bỏ phần chưa lưu rồi đọc lại dòng của bên thắng.</summary>
    public static async Task<UserExpenditureSettings> GetOrCreateAsync(IUnitOfWork uow, Guid userId, CancellationToken ct)
    {
        var repo = uow.Repository<UserExpenditureSettings>();
        var settings = await repo.FindTrackedAsync(s => s.UserId == userId, ct);
        if (settings is not null) return settings;

        try
        {
            settings = new UserExpenditureSettings { Id = Guid.NewGuid(), UserId = userId };
            await repo.AddAsync(settings, ct);
            await uow.SaveChangesAsync(ct);
            return settings;
        }
        catch (DbUpdateException)
        {
            uow.DiscardChanges();
            return await repo.FindTrackedAsync(s => s.UserId == userId, ct) ?? throw new ConflictException(ExceptionMessages.ExpenditureSettingsBusy);
        }
    }

    public static async Task<ExpenditureSettingsResponse> BuildResponseAsync(IUnitOfWork uow, Guid userId, DateOnly today, CancellationToken ct)
    {
        var settings = await uow.Repository<UserExpenditureSettings>().FindAsync(s => s.UserId == userId, ct);
        var profile = await uow.Repository<UserProfile>().FindAsync(p => p.UserId == userId, ct)
            ?? throw new NotFoundException(ExceptionMessages.NotFound("UserProfile", userId));
        var periods = await uow.Repository<ExpenditureModePeriod>().Query().Where(p => p.UserId == userId).ToListAsync(ct);

        // Fallback đọc từ program MỚI NHẤT (như B3/CreateProgram); không bao giờ coi "program đang active" là đúng, không ghi gì
        var legacy = await uow.Repository<MacroProgram>().Query()
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.StartDate)
            .Select(p => new { p.ManualInitialTdeeKcal, p.ExpenditureStartDate })
            .FirstOrDefaultAsync(ct);

        int? manual = settings?.ManualInitialTdeeKcal;
        string? manualSource = manual is null ? null : "Settings";
        if (settings?.InitialEstimateMode is null && legacy?.ManualInitialTdeeKcal is { } legacyManual) { manual = legacyManual; manualSource = "LegacyProgram"; }

        var startDate = settings?.CalculationStartDate;
        string? startSource = startDate is null ? null : "Settings";
        if (startDate is null && legacy is not null) { startDate = legacy.ExpenditureStartDate; startSource = "LegacyProgram"; }

        var (mode, staticTdee) = ExpenditureModeTimeline.ModeAt(periods, today);

        return new ExpenditureSettingsResponse(
            settings?.InitialEstimateMode?.ToString(), manual, manualSource,
            (settings?.BmrEquation ?? BmrEquationPreference.MacroFactor).ToString(), settings?.BmrSex?.ToString(),
            profile.AthleticPursuits?.ToString(),
            profile.WeighedMoreThanBefore?.ToString(), profile.WeighedMoreThanBeforeAtWeightKg,
            startDate, startSource, settings?.CalculationRevision ?? 0,
            mode.ToString(), staticTdee,
            profile.WeighedMoreThanBeforeThresholdKg, profile.WeighedMoreThanBeforeThresholdShown, profile.WeighedMoreThanBeforeThresholdUnit,
            PriorWeightAdaptation.Eligibility(profile.WeighedMoreThanBefore, profile.WeighedMoreThanBeforeThresholdKg, profile.WeighedMoreThanBeforeThresholdShown, profile.WeighedMoreThanBeforeThresholdUnit, profile.WeighedMoreThanBeforeAtWeightKg).ToString(),
            settings?.DefaultCalculationStartDate);
    }

    /// <summary>Tham số sinh học user đã xác nhận cho Equation 1/Mifflin (null nếu chưa có).</summary>
    public static Task<BiologicalSex?> BmrSexAsync(IUnitOfWork uow, Guid userId, CancellationToken ct)
        => uow.Repository<UserExpenditureSettings>().Query().Where(s => s.UserId == userId).Select(s => s.BmrSex).FirstOrDefaultAsync(ct);

    /// <summary>Cân gần nhất (null nếu chưa có): cân hiện tại để dựng ngưỡng Prior Weight và lưu ngữ cảnh lúc trả lời.</summary>
    public static Task<decimal?> LatestWeightAsync(IUnitOfWork uow, Guid userId, CancellationToken ct)
        => uow.Repository<WeightEntry>().Query()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.LogDate)
            .Select(x => (decimal?)x.WeightKg)
            .FirstOrDefaultAsync(ct);
}
