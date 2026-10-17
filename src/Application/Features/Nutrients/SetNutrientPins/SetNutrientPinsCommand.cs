namespace Application;

/// <summary>UC-D09: đặt danh sách nutrient pin lên Dashboard theo thứ tự (tối đa 8); thay toàn bộ pin hiện có. Danh sách rỗng = bỏ hết pin.</summary>
public sealed record SetNutrientPinsCommand(IReadOnlyList<string> Codes) : IRequest<Unit>;

public sealed class SetNutrientPinsCommandValidator : AbstractValidator<SetNutrientPinsCommand>
{
    public SetNutrientPinsCommandValidator()
    {
        RuleFor(x => x.Codes).NotNull().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.Codes).Must(c => c.Count <= 8).WithMessage("Tối đa 8 nutrient được pin.").When(x => x.Codes is not null);
        RuleForEach(x => x.Codes).NotEmpty().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.Codes)
            .Must(c => c.Select(s => s.Trim().ToLowerInvariant()).Distinct().Count() == c.Count)
            .WithMessage("Code bị trùng.")
            .When(x => x.Codes is not null);
    }
}

public sealed class SetNutrientPinsCommandHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<SetNutrientPinsCommand, Unit>
{
    public async Task<Unit> Handle(SetNutrientPinsCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var codes = request.Codes.Select(c => c.Trim().ToLowerInvariant()).ToList();
        if (codes.Any(NutrientCatalog.MacroCodes.Contains))
            throw new BadRequestException("Calories/Protein/Carbs/Fat đã có tile riêng, không pin ở đây.");

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
            // Dòng không pin và không có target tuỳ chỉnh thì bỏ
            var empty = rows.Where(r => r.PinOrder is null && r.Floor is null && r.Target is null && r.Ceiling is null).ToList();
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
