namespace Application;

public sealed class SetNutrientPinsCommandHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<SetNutrientPinsCommand, Unit>
{
    public async Task<Unit> Handle(SetNutrientPinsCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var codes = request.Codes.Select(c => c.Trim().ToLowerInvariant()).ToList();
        if (codes.Any(NutrientCatalog.MacroCodes.Contains))
            throw new BadRequestException(ExceptionMessages.MacroNutrientNotPinnable);

        var defs = await _uow.Repository<NutrientDefinition>().GetAllAsync(d => codes.Contains(d.Code), ct);
        if (codes.Except(defs.Select(d => d.Code)).FirstOrDefault() is { } missing)
            throw new NotFoundException(ExceptionMessages.NotFoundField("NutrientDefinition", "Code", missing));

        var repo = _uow.Repository<UserNutrientTarget>();
        var rows = (await repo.GetAllTrackedAsync(t => t.UserId == userId, ct)).ToList();

        await _uow.BeginTransactionAsync(ct);
        try
        {
            // Bước 1: bỏ pin cũ rồi lưu — đổi thứ tự trong cùng một lần lưu sẽ đụng unique (UserId, PinOrder) giữa chừng
            foreach (var r in rows.Where(r => r.PinOrder is not null)) r.PinOrder = null;
            await _uow.SaveChangesAsync(ct);

            var byDef = rows.ToDictionary(r => r.NutrientDefinitionId);
            for (var i = 0; i < codes.Count; i++)
            {
                var def = defs.First(d => d.Code == codes[i]);
                if (byDef.TryGetValue(def.Id, out var row)) row.PinOrder = (byte)(i + 1);
                else await repo.AddAsync(new UserNutrientTarget { Id = Guid.NewGuid(), UserId = userId, NutrientDefinitionId = def.Id, PinOrder = (byte)(i + 1) }, ct);
            }
            // Dòng không còn pin thì bỏ (target tuỳ chỉnh nằm ở UserNutrientTargetHistory, không ở đây)
            var empty = rows.Where(r => r.PinOrder is null).ToList();
            if (empty.Count > 0) await repo.RemoveRangeAsync(empty, ct);

            await _uow.SaveChangesAsync(ct);
            await _uow.CommitTransactionAsync(ct);
        }
        catch
        {
            await _uow.RollbackTransactionAsync(ct);
            throw;
        }
        return Unit.Value;
    }
}
