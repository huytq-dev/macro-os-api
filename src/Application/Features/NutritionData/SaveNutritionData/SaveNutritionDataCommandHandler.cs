namespace Application;

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
        var live = NutritionDataSupport.Live(log);
        var manualGiven = request.Calories is not null || request.ProteinG is not null || request.CarbsG is not null || request.FatG is not null;

        bool changed;
        if (manualGiven)
        {
            // Override của ngày = đúng các giá trị gửi lên; không đụng entry/ghi chú/nước (nên không đủ điều kiện "ngày trống" ở bước dọn cờ bên dưới)
            changed = NutritionDataSupport.ApplyManual(log, live, request.Calories, request.ProteinG, request.CarbsG, request.FatG, request.IsFasting, request.IsIncomplete);
        }
        else
        {
            // Không có trường manual nào: chỉ đổi cờ. Tất cả trống mà không đổi gì = KHÔNG xoá override, KHÔNG báo thành công (xoá override là thao tác riêng)
            var before = (log.IsFasting, log.IsIncomplete);
            if (request.IsFasting == true)
            {
                if (live.Count > 0) throw NutritionDataSupport.FastingBlocked();
                if (NutritionDataSupport.ManualPositive(log.ManualCaloriesKcal, log.ManualProteinG, log.ManualCarbsG, log.ManualFatG)) throw NutritionDataSupport.FastingWithManual();
                log.IsFasting = true;
            }
            else if (request.IsFasting == false) log.IsFasting = false;

            if (request.IsIncomplete is { } incomplete)
            {
                if (incomplete && NutritionDataSupport.IsEmptyDay(log, live)) throw new BadRequestException(ExceptionMessages.IncompleteNeedsLoggedDay);
                log.IsIncomplete = incomplete;
            }
            changed = before != (log.IsFasting, log.IsIncomplete);
            if (!changed) throw NutritionDataSupport.HasManual(log) ? NutritionDataSupport.UseRemoveOverride() : NutritionDataSupport.NothingToSave();
        }

        if (NutritionDataSupport.ClearStaleIncomplete(log, live)) changed = true;   // ngày vừa thành trống thật (vd. bỏ nhịn ăn) không giữ cờ chưa-đủ cũ
        if (changed) await FoodLogSupport.SaveAndRebuildAsync(_uow, userId, log.LogDate, _options.Value, _timeProvider, ct);
        return NutritionDataSupport.ToModel(log, live);
    }
}
