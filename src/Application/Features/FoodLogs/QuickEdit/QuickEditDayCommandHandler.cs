namespace Application;

public sealed class QuickEditDayCommandHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider, IOptions<AlgorithmOptions> _options)
    : IRequestHandler<QuickEditDayCommand, Guid>
{
    public async Task<Guid> Handle(QuickEditDayCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var log = await FoodLogSupport.GetOrCreateAsync(_uow, userId, request.Date, ct);
        var live = NutritionDataSupport.Live(log);

        // Quick Edit = Edit Day của MF: đặt Manual Nutrition CỦA NGÀY (4 cột riêng), không bao giờ đụng món/ghi chú/nước; override thắng Food Log, không cộng.
        // Mọi giá trị là số người dùng nhập (kể cả 0). Nhịn ăn + manual dương ⇒ 400; đúng số đang lưu ⇒ không ghi gì
        var changed = NutritionDataSupport.ApplyManual(log, live, request.Calories, request.Protein, request.Carbs, request.Fat, fasting: null, incomplete: null);
        if (changed) await FoodLogSupport.SaveAndRebuildAsync(_uow, userId, log.LogDate, _options.Value, _timeProvider, ct);
        return log.Id;
    }
}
