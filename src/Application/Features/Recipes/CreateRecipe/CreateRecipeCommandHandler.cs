using System.Text.Json;

namespace Application;

public sealed class CreateRecipeCommandHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<CreateRecipeCommand, Guid>
{
    public async Task<Guid> Handle(CreateRecipeCommand request, CancellationToken ct)
    {
        var recipe = new Recipe
        {
            Id = Guid.NewGuid(),
            CreatedByUserId = _userContext.UserId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            ImageUrl = request.ImageUrl?.Trim(),
            ServingsCount = request.ServingsCount,
            PrepTimeMinutes = request.PrepTimeMinutes,
            CookTimeMinutes = request.CookTimeMinutes,
            MealType = request.MealType ?? MealType.Lunch,
            SourceUrl = request.SourceUrl?.Trim(),
            IconKey = request.IconKey,
            StepsJson = RecipeSteps.Serialize(request.Steps),
        };
        var version = await RecipeWriter.ApplyAsync(_uow, recipe.CreatedByUserId, recipe, 1, request.Instructions, request.CookedYieldGrams, request.Ingredients, ct);

        await _uow.Repository<Recipe>().AddAsync(recipe, ct);
        await _uow.Repository<RecipeVersion>().AddAsync(version, ct);
        await _uow.EnsureSaveAsync(ct);

        // Recipe ↔ RecipeVersion tham chiếu vòng nhau: lưu hai bên trước rồi mới trỏ CurrentVersionId
        recipe.CurrentVersionId = version.Id;
        await _uow.EnsureSaveAsync(ct);
        return recipe.Id;
    }
}
