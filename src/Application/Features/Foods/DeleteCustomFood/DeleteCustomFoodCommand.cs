namespace Application;

public sealed record DeleteCustomFoodCommand(Guid Id) : IRequest<Unit>;

public sealed class DeleteCustomFoodCommandHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<DeleteCustomFoodCommand, Unit>
{
    public async Task<Unit> Handle(DeleteCustomFoodCommand request, CancellationToken ct)
    {
        var food = await _uow.Repository<FoodItem>().FindTrackedAsync(f => f.Id == request.Id, ct)
            ?? throw new NotFoundException(ExceptionMessages.NotFound("FoodItem", request.Id));

        if (food.Source != FoodSource.UserCreated || food.CreatedByUserId != _userContext.UserId)
            throw new ForbiddenException("Chỉ xoá được thực phẩm do bạn tạo.");

        // Soft delete qua interceptor; FoodLogEntry cũ vẫn giữ snapshot
        await _uow.Repository<FoodItem>().RemoveAsync(food, ct);
        await _uow.EnsureSaveAsync(ct);
        return Unit.Value;
    }
}
