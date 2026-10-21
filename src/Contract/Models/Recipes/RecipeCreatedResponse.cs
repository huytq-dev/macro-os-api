namespace Contract;

/// <param name="EntryId">Entry food log vừa tạo khi "Create &amp; Add".</param>
public sealed record RecipeCreatedResponse(Guid RecipeId, Guid? EntryId);
