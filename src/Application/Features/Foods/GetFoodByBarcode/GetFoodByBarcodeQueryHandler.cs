namespace Application;

public sealed class GetFoodByBarcodeQueryHandler(IUnitOfWork _uow, IUserContext _userContext, IOpenFoodFactsClient _off)
    : IRequestHandler<GetFoodByBarcodeQuery, FoodItemResponse>
{
    public async Task<FoodItemResponse> Handle(GetFoodByBarcodeQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var code = request.Code.Trim();

        // Custom food của người khác và món quick add ẩn coi như không tồn tại; ưu tiên món của mình rồi món đã xác minh
        var food = await _uow.Repository<FoodItem>().Query()
            .Include(f => f.Servings)
            .Where(f => f.Barcode == code && f.Source != FoodSource.QuickAdd
                        && (f.Source != FoodSource.UserCreated || f.CreatedByUserId == userId))
            .OrderByDescending(f => f.CreatedByUserId == userId).ThenByDescending(f => f.IsVerified)
            .FirstOrDefaultAsync(ct);
        if (food is not null)
            return await FoodNutrientStore.ToResponseAsync(_uow, food, food.CreatedByUserId == userId, userId, ct);

        var product = await _off.FindByBarcodeAsync(code, ct)
            ?? throw new NotFoundException(ExceptionMessages.NotFoundField("FoodItem", "Barcode", code));

        food = new FoodItem
        {
            Id = Guid.NewGuid(),
            Name = product.Name,
            Brand = product.Brand,
            Barcode = code,
            Category = FoodCategory.Other,
            Source = FoodSource.OpenFoodFacts,
            IsVerified = false,
            QualityStatus = FoodQualityStatus.Unverified,
            ProviderCode = "off",
            ExternalFoodId = code,
            ImageUrl = product.ImageUrl,
            IconKey = IconKeyResolver.Resolve(product.Name),
        };
        FoodNutritionApplier.Apply(food, product.Nutrition);

        // Khẩu phần theo nhãn (nếu có) là mặc định; luôn có "100 g"
        List<FoodServingInput> servings = product.ServingGrams is { } grams
            ? [new(product.ServingName ?? $"{grams:0.##} g", grams, true), new("100 g", 100, false)]
            : [new("100 g", 100, true)];
        FoodNutritionApplier.ReplaceServings(food, servings);

        await _uow.Repository<FoodItem>().AddAsync(food, ct);
        await CanonicalLinker.LinkAsync(_uow, [food], ct);
        await FoodNutrientStore.SaveAsync(_uow, food.Id, FoodNutrientStore.Amounts(product.Nutrition, null),
            replaceAll: true, isNew: true, await NutrientSnapshot.CodeIdsAsync(_uow, ct), ct);
        await _uow.SaveChangesAsync(ct);
        return await FoodNutrientStore.ToResponseAsync(_uow, food, isMine: false, userId, ct);
    }
}
