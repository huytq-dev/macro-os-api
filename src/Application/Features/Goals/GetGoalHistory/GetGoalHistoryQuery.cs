namespace Application;

/// <summary>Mọi goal của user, mới nhất trước (goal cũ giữ nguyên khi có goal mới). Dùng cho mục "Goal History" của tab Strategy.</summary>
public sealed record GetGoalHistoryQuery : IRequest<IReadOnlyList<GoalHistoryItemModel>>;

public sealed class GetGoalHistoryQueryHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<GetGoalHistoryQuery, IReadOnlyList<GoalHistoryItemModel>>
{
    public async Task<IReadOnlyList<GoalHistoryItemModel>> Handle(GetGoalHistoryQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        // Program tạo trước S9 chưa có Goal: dựng goal từ program để lịch sử không rỗng
        await GoalSupport.EnsureActiveGoalAsync(_uow, userId, ct);

        var rows = await _uow.Repository<Goal>().Query()
            .Where(g => g.UserId == userId)
            .OrderByDescending(g => g.StartDate).ThenByDescending(g => g.CreatedAt).ThenBy(g => g.Id)
            .Take(AppConstants.MaxPageSize)
            .Select(g => new { g.Id, g.Type, g.TargetWeightKg, g.StartWeightKg, g.RatePerWeekKg, g.StartDate, g.EndDate, g.Status })
            .ToListAsync(ct);

        return rows.Select(g => new GoalHistoryItemModel(
            g.Id, g.Type.ToString(), g.TargetWeightKg, g.StartWeightKg, g.RatePerWeekKg, g.StartDate, g.EndDate, g.Status.ToString())).ToList();
    }
}
