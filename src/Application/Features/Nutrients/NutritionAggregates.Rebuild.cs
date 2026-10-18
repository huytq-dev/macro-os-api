namespace Application;

/// <summary>WP5: dựng lại NutritionDayAggregate của user trong khoảng ngày (mặc định 365 ngày gần nhất) — backfill dữ liệu cũ hoặc sau khi sửa danh mục chất. Trả số ngày có dữ liệu.</summary>
public sealed record RebuildNutritionAggregatesCommand(DateOnly? From, DateOnly? To) : IRequest<int>;

public sealed class RebuildNutritionAggregatesCommandValidator : AbstractValidator<RebuildNutritionAggregatesCommand>
{
    public RebuildNutritionAggregatesCommandValidator()
        => RuleFor(x => x).Must(x => x.From is null || x.To is null || (x.To >= x.From && x.To.Value.DayNumber - x.From.Value.DayNumber <= 730))
            .WithName("From").WithMessage("Khoảng ngày không hợp lệ (tối đa 730 ngày).");
}

public sealed class RebuildNutritionAggregatesCommandHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<RebuildNutritionAggregatesCommand, int>
{
    public async Task<int> Handle(RebuildNutritionAggregatesCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var to = request.To ?? FoodLogSupport.Today(_timeProvider);
        var from = request.From ?? to.AddDays(-364);

        var logged = await FoodLogSupport.LoadDaysAsync(_uow, userId, from, to, ct);
        // Ngày không còn entry nhưng còn aggregate cũ cũng phải dọn: thêm vào danh sách
        var stale = await _uow.Repository<NutritionDayAggregate>().Query()
            .Where(a => a.UserId == userId && a.LocalDate >= from && a.LocalDate <= to)
            .Select(a => a.LocalDate).ToListAsync(ct);
        var dates = logged.Keys.Union(stale).ToList();

        await NutritionAggregates.RefreshAsync(_uow, userId, dates, _timeProvider, ct);
        await _uow.SaveChangesAsync(ct);
        return logged.Count;
    }
}
