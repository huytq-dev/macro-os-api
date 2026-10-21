namespace Application;

public sealed record AiMealDraft(IReadOnlyList<AiDraftDish> Dishes, string? Confidence);
