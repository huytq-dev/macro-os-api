namespace Application;

/// <summary>UC-D09: ghi đè floor/target/ceiling của một nutrient (theo Code). Cả ba null = về mặc định. Calories/P/F/C đặt qua program.</summary>
public sealed record UpdateNutrientTargetCommand(
    // Nullable: route gán Code sau khi bind body; non-nullable thì model validation báo "Code field is required" trước controller.
    [property: System.Text.Json.Serialization.JsonIgnore] string? Code,
    decimal? Floor,
    decimal? Target,
    decimal? Ceiling
) : IRequest<Unit>;

public sealed class UpdateNutrientTargetCommandValidator : AbstractValidator<UpdateNutrientTargetCommand>
{
    public UpdateNutrientTargetCommandValidator()
    {
        RuleFor(x => x.Floor).GreaterThanOrEqualTo(0).When(x => x.Floor.HasValue);
        RuleFor(x => x.Target).GreaterThanOrEqualTo(0).When(x => x.Target.HasValue);
        RuleFor(x => x.Ceiling).GreaterThanOrEqualTo(0).When(x => x.Ceiling.HasValue);
        RuleFor(x => x).Must(x =>
                (x.Floor is null || x.Target is null || x.Floor <= x.Target)
                && (x.Target is null || x.Ceiling is null || x.Target <= x.Ceiling)
                && (x.Floor is null || x.Ceiling is null || x.Floor <= x.Ceiling))
            .WithMessage("Phải đúng thứ tự Floor ≤ Target ≤ Ceiling.");
    }
}

public sealed class UpdateNutrientTargetCommandHandler(IUnitOfWork _uow, IUserContext _userContext)
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

        var repo = _uow.Repository<UserNutrientTarget>();
        var row = await repo.FindTrackedAsync(t => t.UserId == userId && t.NutrientDefinitionId == def.Id, ct);
        var clearing = request.Floor is null && request.Target is null && request.Ceiling is null;

        if (row is null)
        {
            if (clearing) return Unit.Value;
            row = new UserNutrientTarget { Id = Guid.NewGuid(), UserId = userId, NutrientDefinitionId = def.Id };
            await repo.AddAsync(row, ct);
        }
        row.Floor = request.Floor;
        row.Target = request.Target;
        row.Ceiling = request.Ceiling;

        // Không pin và không còn target tuỳ chỉnh → bỏ dòng (về mặc định)
        if (clearing && row.PinOrder is null) await repo.RemoveAsync(row, ct);

        await _uow.SaveChangesAsync(ct);
        return Unit.Value;
    }
}
