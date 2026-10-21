using System.Text.Json;

namespace Application;

public sealed class DeleteRecipeCommandHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<DeleteRecipeCommand, Unit>
{
    public async Task<Unit> Handle(DeleteRecipeCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var recipe = await _uow.Repository<Recipe>().FindTrackedAsync(r => r.Id == request.Id && r.CreatedByUserId == userId, ct)
            ?? throw new NotFoundException(ExceptionMessages.NotFound("Recipe", request.Id));
        await _uow.Repository<Recipe>().RemoveAsync(recipe, ct); // soft delete qua interceptor
        await _uow.EnsureSaveAsync(ct);
        return Unit.Value;
    }
}
