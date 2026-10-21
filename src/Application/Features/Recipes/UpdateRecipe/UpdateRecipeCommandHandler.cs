using System.Text.Json;

namespace Application;

public sealed class UpdateRecipeCommandHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<UpdateRecipeCommand, Unit>
{
    public async Task<Unit> Handle(UpdateRecipeCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var recipe = await _uow.Repository<Recipe>().Query(tracking: true)
            .Include(r => r.Ingredients)
            .FirstOrDefaultAsync(r => r.Id == request.Id && r.CreatedByUserId == userId, ct)
            ?? throw new NotFoundException(ExceptionMessages.NotFound("Recipe", request.Id));

        recipe.Name = request.Name.Trim();
        recipe.Description = request.Description?.Trim();
        recipe.ImageUrl = request.ImageUrl?.Trim();
        recipe.ServingsCount = request.ServingsCount;
        recipe.PrepTimeMinutes = request.PrepTimeMinutes;
        recipe.CookTimeMinutes = request.CookTimeMinutes;
        recipe.MealType = request.MealType ?? recipe.MealType;
        recipe.SourceUrl = request.SourceUrl?.Trim();
        recipe.IconKey = request.IconKey; // gửi đủ object: null = bỏ icon đã chọn
        recipe.StepsJson = RecipeSteps.Serialize(request.Steps); // null/rỗng = xoá hết bước

        var last = await _uow.Repository<RecipeVersion>().Query().Where(v => v.RecipeId == recipe.Id).MaxAsync(v => (int?)v.VersionNo, ct) ?? 0;
        recipe.Ingredients.Clear(); // FK bắt buộc: EF xoá các dòng bị bỏ khỏi collection
        var version = await RecipeWriter.ApplyAsync(_uow, userId, recipe, last + 1, request.Instructions, request.CookedYieldGrams, request.Ingredients, ct);

        await _uow.Repository<RecipeVersion>().AddAsync(version, ct);
        await _uow.EnsureSaveAsync(ct);
        recipe.CurrentVersionId = version.Id;
        await _uow.EnsureSaveAsync(ct);
        return Unit.Value;
    }
}
