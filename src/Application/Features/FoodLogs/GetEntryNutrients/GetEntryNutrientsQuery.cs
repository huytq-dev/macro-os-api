namespace Application;

/// <summary>F10: mọi chất của một entry đã log, đọc từ snapshot lúc log (macro/fiber/sodium từ cột entry, còn lại từ FoodLogEntryNutrient).</summary>
public sealed record GetEntryNutrientsQuery(Guid Id) : IRequest<EntryNutrientsResponse>;
