namespace Application;

public sealed class UpdateNutrientTargetCommandHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<UpdateNutrientTargetCommand, Unit>
{
    public async Task<Unit> Handle(UpdateNutrientTargetCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var code = (request.Code ?? "").Trim().ToLowerInvariant();
        if (NutrientCatalog.MacroCodes.Contains(code))
            throw new BadRequestException("Calories/Protein/Carbs/Fat đặt qua program, không đặt ở đây.");

        var def = await _uow.Repository<NutrientDefinition>().FindAsync(d => d.Code == code, ct)
            ?? throw new NotFoundException(ExceptionMessages.NotFoundField("NutrientDefinition", "Code", code));

        // Mỗi lần đổi = một dòng lịch sử có hiệu lực từ HÔM NAY (ngày local của user); ngày cũ giữ mức cũ. Cả ba null = về mặc định từ hôm nay (cũng là một dòng).
        var today = (await UserLocale.GetAsync(_uow, userId, _timeProvider, ct)).Today;
        var repo = _uow.Repository<UserNutrientTargetHistory>();
        var rows = (await repo.GetAllTrackedAsync(h => h.UserId == userId && h.NutrientDefinitionId == def.Id, ct)).OrderBy(h => h.EffectiveFrom).ToList();
        var changes = rows.Select(r => new GoalChange(r.EffectiveFrom, r.Floor, r.Target, r.Ceiling)).ToList();
        var wanted = new UserGoal(request.Floor, request.Target, request.Ceiling);

        // Không đổi gì so với mức đang hiệu lực hôm nay (lưu lại y nguyên / reset khi đã là mặc định)
        var current = NutrientGoalHistory.On(changes, today);
        if (current is null ? wanted.IsEmpty : current == wanted) return Unit.Value;

        var todayRow = rows.FirstOrDefault(r => r.EffectiveFrom == today);
        if (todayRow is null)
            await repo.AddAsync(new UserNutrientTargetHistory
            {
                Id = Guid.NewGuid(), UserId = userId, NutrientDefinitionId = def.Id, EffectiveFrom = today,
                Floor = request.Floor, Target = request.Target, Ceiling = request.Ceiling,
            }, ct);
        else if (wanted.IsEmpty && NutrientGoalHistory.On(changes.Where(c => c.EffectiveFrom < today).ToList(), today) is null)
            await repo.RemoveAsync(todayRow, ct); // trước hôm nay vốn là mặc định: bỏ dòng hôm nay thay vì giữ dòng rỗng
        else
            (todayRow.Floor, todayRow.Target, todayRow.Ceiling) = (request.Floor, request.Target, request.Ceiling);

        await _uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
