using System.Text.Json;

namespace Application;

public sealed class CreateAndLogRecipeCommandHandler(ISender _sender) : IRequestHandler<CreateAndLogRecipeCommand, RecipeCreatedResponse>
{
    public async Task<RecipeCreatedResponse> Handle(CreateAndLogRecipeCommand request, CancellationToken ct)
    {
        var recipeId = await _sender.Send(request.Recipe, ct); // qua pipeline: validate + lưu
        var log = request.Log ?? new RecipeLogOptions(null, null, null);
        var entryId = await _sender.Send(new AddFoodLogEntryCommand(log.LogDate, log.MealType, log.EatenAt, null, null, log.Quantity, recipeId), ct);
        return new RecipeCreatedResponse(recipeId, entryId);
    }
}
