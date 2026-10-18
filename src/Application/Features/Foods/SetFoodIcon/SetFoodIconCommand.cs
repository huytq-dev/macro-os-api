namespace Application;

/// <summary>
/// Đổi icon của một món (bảng "Choose Icon"). Món tự tạo của tôi: ghi thẳng vào món; món hệ thống/USDA/OFF: lưu icon riêng của user (UserFoodIcon).
/// IconKey null = bỏ lựa chọn: món hệ thống xoá icon riêng (về icon của món), món của tôi về icon tự gán theo tên (có thể null). Trả iconKey hiệu lực sau khi đổi.
/// </summary>
public sealed record SetFoodIconCommand(
    [property: System.Text.Json.Serialization.JsonIgnore] Guid Id,
    string? IconKey
) : IRequest<FoodIconResponse>;

public sealed class SetFoodIconCommandValidator : AbstractValidator<SetFoodIconCommand>
{
    public SetFoodIconCommandValidator()
        => RuleFor(x => x.IconKey).Must(FoodIconKeys.IsValid).When(x => x.IconKey is not null).WithMessage(ValidationMessages.InvalidIconKey);
}

public sealed class SetFoodIconCommandHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<SetFoodIconCommand, FoodIconResponse>
{
    public async Task<FoodIconResponse> Handle(SetFoodIconCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        // 404 nếu món không tồn tại hoặc là món riêng của người khác
        var food = await FoodLogSupport.GetLoggableFoodAsync(_uow, userId, request.Id, ct);

        if (food.Source == FoodSource.UserCreated && food.CreatedByUserId == userId)
        {
            var own = await _uow.Repository<FoodItem>().FindTrackedAsync(f => f.Id == food.Id, ct)
                ?? throw new NotFoundException(ExceptionMessages.NotFound("FoodItem", food.Id));
            own.IconKey = request.IconKey ?? IconKeyResolver.Resolve(own.Name, own.Category);
            await _uow.EnsureSaveAsync(ct);
            return new FoodIconResponse(own.IconKey);
        }

        var repo = _uow.Repository<UserFoodIcon>();
        var existing = await repo.FindTrackedAsync(i => i.UserId == userId && i.FoodItemId == food.Id, ct);
        if (request.IconKey is null)
        {
            if (existing is not null) await repo.RemoveAsync(existing, ct);
        }
        else if (existing is not null)
            existing.IconKey = request.IconKey;
        else
            await repo.AddAsync(new UserFoodIcon { Id = Guid.NewGuid(), UserId = userId, FoodItemId = food.Id, IconKey = request.IconKey }, ct);

        await _uow.EnsureSaveAsync(ct);
        return new FoodIconResponse(request.IconKey ?? food.IconKey);
    }
}
