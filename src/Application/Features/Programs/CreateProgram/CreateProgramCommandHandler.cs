namespace Application;

public sealed class CreateProgramCommandHandler(
    IUnitOfWork _uow,
    IUserContext _userContext,
    IOptions<AlgorithmOptions> _options,
    TimeProvider _timeProvider
) : IRequestHandler<CreateProgramCommand, Guid>
{
    public async Task<Guid> Handle(CreateProgramCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var today = (await UserLocale.GetAsync(_uow, userId, _timeProvider, ct)).Today;   // ngày LOCAL của user (cùng ngày với Preview, tuổi, StartDate)

        var profile = await _uow.Repository<UserProfile>().FindTrackedAsync(p => p.UserId == userId, ct)
            ?? throw new NotFoundException(ExceptionMessages.NotFound("UserProfile", userId));

        if (profile.DateOfBirth is null || profile.HeightCm is null)
            throw new BadRequestException(ExceptionMessages.ProfileRequiredForProgram);

        var latestWeight = await _uow.Repository<WeightEntry>().Query()
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.LogDate)
            .Select(w => (decimal?)(w.TrendWeightKg ?? w.WeightKg))
            .FirstOrDefaultAsync(ct)
            ?? throw new BadRequestException(ExceptionMessages.WeightRequiredForProgram);

        // Chỉ một program active (BR-01): kết thúc program cũ, giữ lịch sử
        var activePrograms = await _uow.Repository<MacroProgram>().GetAllTrackedAsync(p => p.UserId == userId && p.IsActive, ct);
        foreach (var old in activePrograms) { old.IsActive = false; old.Status = NutritionProgramStatus.Superseded; old.EndedAt = today.AddDays(-1); } // ngày cuối của program cũ = hôm qua; hôm nay thuộc program mới (ProgramHistory bị chặn theo EndedAt)

        var program = ProgramDraftCalculator.BuildProgram(request, profile, latestWeight, today);

        if (request.TrainingType is { } training) profile.TrainingType = training;
        // Cùng resolver với Setup và Preview để số liệu khớp: expenditure hiện tại trước, rồi TDEE nhập tay khởi điểm, rồi công thức (Cunningham/Mifflin)
        var tdee = (await InitialTdeeResolver.ResolveAsync(_uow, userId, profile, latestWeight, request.ManualInitialTdeeKcal, today, _options.Value, ct)).TdeeKcal;

        // Đã có expenditure (học hoặc seed) thì program mới KẾ THỪA ngày bắt đầu của nó: ExpenditureStartDate chỉ đổi khi user tự reset
        var latestSnapshot = await _uow.Repository<ExpenditureSnapshot>().Query()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.SnapshotDate)
            .Select(s => (DateOnly?)s.SnapshotDate)
            .FirstOrDefaultAsync(ct);
        if (latestSnapshot is not null)
        {
            var previousStart = await _uow.Repository<MacroProgram>().Query()
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.StartDate)
                .Select(p => (DateOnly?)p.ExpenditureStartDate)
                .FirstOrDefaultAsync(ct);
            if (previousStart is { } inherited) program.ExpenditureStartDate = inherited;
        }

        if (request.DayTargets is { Count: > 0 })
        {
            // User tự đặt từng ngày (Collaborative/Manual): lưu 7 dòng; target tuần = trung bình 7 ngày để chỗ chỉ biết target tuần vẫn đúng
            var days = ProgramDraftCalculator.Days(request, program, latestWeight, tdee, _options.Value);
            program.DayTargets = ProgramDayTargets.FromInputs(program.Id, days);
            ProgramWeekBuilder.UpsertWeek(program, today, ProgramDayTargets.Average(program.DayTargets), AdjustmentReason.Initial);
        }
        else
        {
            if (program.ProgramType == ProgramType.Manual) throw new BadRequestException(ExceptionMessages.ManualProgramNeedsSevenDays);
            var targets = ProgramWeekBuilder.Compute(program, latestWeight, tdee, _options.Value);
            // UpsertWeek kiểm khả thi (kcal > 0, macro ≤ kcal, shift) TRƯỚC khi thêm bất cứ thứ gì và từ chối 400 + FeasibilityResponse: Create không ghi dở
            ProgramWeekBuilder.UpsertWeek(program, today, targets, AdjustmentReason.Initial);
        }

        // Goal là nguồn cân nặng mục tiêu: đóng goal cũ, tạo goal mới gắn với program (UC-B08)
        foreach (var previous in await _uow.Repository<Goal>().GetAllTrackedAsync(g => g.UserId == userId && g.Status == GoalStatus.Active, ct))
        {
            previous.Status = GoalStatus.Superseded;
            previous.EndDate = today;
        }
        var goal = new Goal
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = request.GoalType,
            TargetWeightKg = request.TargetWeightKg,
            StartWeightKg = latestWeight,
            RatePerWeekKg = program.WeeklyWeightChangeTargetKg,
            StartDate = today,
            Status = GoalStatus.Active,
        };
        await _uow.Repository<Goal>().AddAsync(goal, ct);
        program.GoalId = goal.Id;

        await _uow.Repository<MacroProgram>().AddAsync(program, ct);

        // Snapshot expenditure khởi tạo (TDEE₀) CHỈ khi user chưa có expenditure nào. Đã có thì KHÔNG ghi đè snapshot hiện tại, không reset học, không chuyển Calibrating
        if (latestSnapshot is null)
        {
            var snapshot = new ExpenditureSnapshot
            {
                Id = Guid.NewGuid(), UserId = userId, SnapshotDate = today, EstimatedTdeeKcal = tdee, Status = ExpenditureStatus.Calibrating,
                AlgorithmVersion = AlgorithmVersions.ExpenditureFor(_options.Value), AvgCaloriesConsumed = 0, WeightChangeRateKgPerWeek = null, WindowDays = 0,
            };
            await _uow.Repository<ExpenditureSnapshot>().AddAsync(snapshot, ct);

            // Expenditure tracking thực sự khởi tạo lần đầu ⇒ lưu ngày bắt đầu mặc định (ngày LOCAL của user), một lần, độc lập program; không bao giờ ghi đè giá trị đã có
            var settings = await _uow.Repository<UserExpenditureSettings>().FindTrackedAsync(s => s.UserId == userId, ct);
            if (settings is null)
                await _uow.Repository<UserExpenditureSettings>().AddAsync(new UserExpenditureSettings { Id = Guid.NewGuid(), UserId = userId, DefaultCalculationStartDate = today }, ct);
            else settings.DefaultCalculationStartDate ??= today;
        }

        await _uow.EnsureSaveAsync(ct);

        // Khoảng trống giữa snapshot cuối và hôm nay (không có program active nên không ai dựng) được dựng bù bằng job bền.
        // Program đã commit ở trên ⇒ không dùng ct của request: client huỷ giữa chừng không được làm mất job (như đường lỗi của DerivedDataSync)
        if (latestSnapshot is { } lastSnapshot && lastSnapshot < today)
        {
            await DerivedDataJobs.EnqueueAsync(_uow, userId, lastSnapshot.AddDays(1), today, RecalculationCause.Manual, null, CancellationToken.None, _timeProvider);
            _uow.Derived.Wake();
        }
        return program.Id;
    }
}
