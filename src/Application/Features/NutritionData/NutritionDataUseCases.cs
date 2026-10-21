namespace Application;

/// <summary>
/// Nutrition Data Manager — danh sách các ngày có dữ liệu dinh dưỡng (mới nhất trước) với tổng kcal/protein/carbs/fat của ngày, nguồn (App | Manual) và cờ fasting/incomplete.
/// Đây là NGUỒN DỮ LIỆU DUY NHẤT về lượng ăn của một ngày: expenditure, check-in, dải ngày, dashboard và habits đều đọc cùng FoodLog + entries (qua <see cref="FoodLogSupport.LoadDaysAsync"/>).
/// From/To tuỳ chọn (gồm hai đầu), mặc định mọi ngày.
/// </summary>
public sealed record GetNutritionDataQuery(DateOnly? From, DateOnly? To) : IRequest<NutritionDataResponse>;

public sealed class GetNutritionDataQueryValidator : AbstractValidator<GetNutritionDataQuery>
{
    public GetNutritionDataQueryValidator()
        => RuleFor(x => x).Must(x => x.From is null || x.To is null || x.From <= x.To).WithName("From").WithMessage("From must not be after To.");
}

public static class NutritionDataSupport
{
    public const string AppSource = "App", ManualSource = "Manual";

    public static string SourceOf(bool fasting, int entryCount, int quickAddCount)
        => fasting || (entryCount > 0 && quickAddCount == entryCount) ? ManualSource : AppSource;

    /// <summary>Ngày có ít nhất một entry không phải Quick Add (Source = App): dữ liệu của Food Log, NDM không được ghi đè.</summary>
    public static bool IsAppDay(IEnumerable<FoodLogEntry> liveEntries) => liveEntries.Any(e => e.Source != EntrySource.QuickAdd);
}

/// <summary>
/// Xoá một ngày từ NDM. Ngày nhập tay / nhịn ăn / chỉ có cờ: xoá cả ngày như DELETE /food-logs/{date}. Ngày Source = App (có món log từ Food Log): 400, không xoá gì — xoá món ở Food Log.
/// 404 nếu user không có ngày đó.
/// </summary>
public sealed record DeleteNutritionDataCommand(DateOnly Date) : IRequest<Unit>;

public sealed class DeleteNutritionDataCommandHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider, IOptions<AlgorithmOptions> _options)
    : IRequestHandler<DeleteNutritionDataCommand, Unit>
{
    public async Task<Unit> Handle(DeleteNutritionDataCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var log = await _uow.Repository<FoodLog>().Query(tracking: true).Include(l => l.Entries)
            .FirstOrDefaultAsync(l => l.UserId == userId && l.LogDate == request.Date, ct)
            ?? throw new NotFoundException(ExceptionMessages.NotFound("FoodLog", request.Date));
        if (NutritionDataSupport.IsAppDay(log.Entries.Where(e => !e.IsDeleted)))
            throw new BadRequestException(ExceptionMessages.DayLoggedInFoodLogDelete);

        await _uow.Repository<FoodLog>().RemoveAsync(log, ct);
        await FoodLogSupport.SaveAndRebuildAsync(_uow, userId, request.Date, _options.Value, _timeProvider, ct);
        return Unit.Value;
    }
}

public sealed class GetNutritionDataQueryHandler(IUnitOfWork _uow, IUserContext _userContext) : IRequestHandler<GetNutritionDataQuery, NutritionDataResponse>
{
    public async Task<NutritionDataResponse> Handle(GetNutritionDataQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var query = _uow.Repository<FoodLog>().Query().Where(l => l.UserId == userId && (l.Entries.Any() || l.IsFasting));
        if (request.From is { } from) query = query.Where(l => l.LogDate >= from);
        if (request.To is { } to) query = query.Where(l => l.LogDate <= to);

        var rows = await query.OrderByDescending(l => l.LogDate)
            .Select(l => new
            {
                l.LogDate, l.TotalCalories, l.TotalProtein, l.TotalCarbs, l.TotalFat, l.IsFasting, l.IsIncomplete,
                Count = l.Entries.Count(),
                Manual = l.Entries.Count(e => e.Source == EntrySource.QuickAdd),
            })
            .ToListAsync(ct);

        var items = rows.Select(r =>
        {
            var fasting = r.IsFasting && r.Count == 0;
            return new NutritionDataDayModel(r.LogDate, fasting ? 0 : r.TotalCalories, fasting ? 0 : r.TotalProtein, fasting ? 0 : r.TotalFat, fasting ? 0 : r.TotalCarbs,
                NutritionDataSupport.SourceOf(fasting, r.Count, r.Manual), fasting, r.IsIncomplete, r.Count);
        }).ToList();
        return new NutritionDataResponse(items);
    }
}

/// <summary>
/// Lưu MỘT ngày từ sheet Nutrition Data Manager (Date trong body hoặc route; không ở tương lai theo múi giờ user):
/// • Ngày có món log từ Food Log (Source = App, có entry không phải Quick Add): KHÔNG ghi đè/xoá món — chỉ đổi được <c>IsIncomplete</c>; gửi Calories/macros hoặc IsFasting = true → 400.
/// • <c>IsFasting</c> = true (ngày trống / nhập tay): Calories/macros phải trống hoặc 0; entry nhập tay của ngày bị xoá mềm, ngày tính là đã log với 0 kcal (không phá chuỗi).
/// • Có <c>Calories</c> (không fasting; ngày trống / nhập tay): tạo/cập nhật đúng MỘT entry nhập tay (Quick Add; macros thiếu = 0) thay các entry nhập tay cũ — tổng ngày = số nhập.
/// • Không có Calories và không fasting: chỉ đổi cờ <c>IsIncomplete</c> của ngày đã có dữ liệu (không đụng entry); ngày chưa có gì thì 400; ngày đang fasting mà bỏ fasting thì thành ngày chưa log.
/// <c>IsIncomplete</c> = true: expenditure và trung bình check-in bỏ qua ngày này. Trả lại ngày sau khi lưu; expenditure/aggregate dựng lại nền như POST entries.
/// </summary>
public sealed record SaveNutritionDataCommand(
    DateOnly? Date,
    decimal? Calories,
    decimal? ProteinG,
    decimal? FatG,
    decimal? CarbsG,
    bool IsFasting = false,
    bool IsIncomplete = false
) : IRequest<NutritionDataDayModel>;

public sealed class SaveNutritionDataCommandValidator : AbstractValidator<SaveNutritionDataCommand>
{
    public SaveNutritionDataCommandValidator()
    {
        RuleFor(x => x.Date).NotNull().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.Calories).InclusiveBetween(0, 10000).When(x => x.Calories.HasValue);
        RuleFor(x => x.ProteinG).InclusiveBetween(0, 1000).When(x => x.ProteinG.HasValue);
        RuleFor(x => x.CarbsG).InclusiveBetween(0, 1000).When(x => x.CarbsG.HasValue);
        RuleFor(x => x.FatG).InclusiveBetween(0, 1000).When(x => x.FatG.HasValue);
        RuleFor(x => x).Must(x => x.Calories.HasValue || (x.ProteinG is null && x.CarbsG is null && x.FatG is null))
            .WithName("Calories").WithMessage("Energy is required when protein, carbs or fat are given.");
        RuleFor(x => x).Must(x => !x.IsFasting || ((x.Calories ?? 0) == 0 && (x.ProteinG ?? 0) == 0 && (x.CarbsG ?? 0) == 0 && (x.FatG ?? 0) == 0))
            .WithName("IsFasting").WithMessage("A fasting day has no energy or macros: leave them empty or 0.");
    }
}

public sealed class SaveNutritionDataCommandHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider, IOptions<AlgorithmOptions> _options)
    : IRequestHandler<SaveNutritionDataCommand, NutritionDataDayModel>
{
    public async Task<NutritionDataDayModel> Handle(SaveNutritionDataCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var date = request.Date!.Value;
        var locale = await UserLocale.GetAsync(_uow, userId, _timeProvider, ct);
        locale.EnsureNotFuture(date, "Date");

        var log = await FoodLogSupport.GetOrCreateAsync(_uow, userId, date, ct);
        var live = log.Entries.Where(e => !e.IsDeleted).ToList();
        // Ngày có món log từ Food Log (không phải Quick Add): không được ghi đè/xoá món ở đây — chỉ đổi được cờ IsIncomplete
        if (NutritionDataSupport.IsAppDay(live) && (request.IsFasting || request.Calories is not null))
            throw new BadRequestException(ExceptionMessages.DayLoggedInFoodLog);

        var replace = request.IsFasting || request.Calories is not null;
        var (wasFasting, wasIncomplete, liveBefore) = (log.IsFasting, log.IsIncomplete, live.Count);

        if (!replace && live.Count == 0 && !log.IsFasting)
            throw new BadRequestException("Energy is required to save a new day (or mark it as fasting).");

        if (replace)
        {
            foreach (var old in live)
            {
                await _uow.Repository<FoodLogEntry>().RemoveAsync(old, ct); // soft delete qua interceptor
                old.IsDeleted = true;
            }
            live.Clear();

            if (request.IsFasting) log.IsFasting = true;
            else
            {
                var food = new FoodItem { Id = Guid.NewGuid(), Name = "Manual entry", Category = FoodCategory.Other, Source = FoodSource.QuickAdd, CreatedByUserId = userId };
                FoodNutritionApplier.Apply(food, new FoodNutrition(request.Calories!.Value, request.ProteinG ?? 0, request.CarbsG ?? 0, request.FatG ?? 0));
                await _uow.Repository<FoodItem>().AddAsync(food, ct);

                var (eatenAt, meal) = await FoodLogSupport.ResolveTimeAsync(_uow, userId, log.LogDate, null, null, _timeProvider, ct);
                var entry = new FoodLogEntry
                {
                    Id = Guid.NewGuid(), FoodLogId = log.Id, MealType = meal, EatenAt = eatenAt, FoodItemId = food.Id,
                    Quantity = 100, // gram; /100 g × 100 g = đúng giá trị nhập
                    Source = EntrySource.QuickAdd,
                };
                FoodLogSupport.Snapshot(entry, food, 100);
                await FoodLogSupport.AddEntryAsync(_uow, log, entry, ct); // tắt cờ fasting
                live.Add(entry);
            }
        }
        else if (log.IsFasting && live.Count == 0) log.IsFasting = false; // bỏ nhịn ăn mà không nhập số: ngày thành chưa log

        log.IsIncomplete = request.IsIncomplete;
        FoodLogSupport.RecalcTotals(log);
        // Lưu lại đúng trạng thái cũ (nhịn ăn → nhịn ăn, cờ không đổi) không có gì để ghi: EnsureSaveAsync sẽ ném lỗi, nên bỏ qua
        var changed = (replace && (!request.IsFasting || liveBefore > 0)) || log.IsFasting != wasFasting || log.IsIncomplete != wasIncomplete;
        if (changed) await FoodLogSupport.SaveAndRebuildAsync(_uow, userId, log.LogDate, _options.Value, _timeProvider, ct);

        var fasting = log.IsFasting && live.Count == 0;
        return new NutritionDataDayModel(log.LogDate, fasting ? 0 : log.TotalCalories, fasting ? 0 : log.TotalProtein, fasting ? 0 : log.TotalFat, fasting ? 0 : log.TotalCarbs,
            NutritionDataSupport.SourceOf(fasting, live.Count, live.Count(e => e.Source == EntrySource.QuickAdd)), fasting, log.IsIncomplete, live.Count);
    }
}
